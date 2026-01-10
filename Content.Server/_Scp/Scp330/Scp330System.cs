using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp330;
using Content.Shared.Body.Part;
using Content.Shared.Gibbing.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp330;

public sealed class Scp330System : SharedScp330System
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<Entity<HandsComponent>> _cachedEntities = [];
    private HashSet<EntityUid> _gibCachedEntities = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp330BowlComponent, InteractHandEvent>(OnActivate);
        SubscribeLocalEvent<Scp330BowlComponent, AfterInteractUsingEvent>(OnAfterInteract);
    }

    #region Event handlers

    private void OnActivate(Entity<Scp330BowlComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTakeCandy(ent, args.User))
            return;

        args.Handled = true;
    }

    private void OnAfterInteract(Entity<Scp330BowlComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryReturnCandy(ent, args.Used))
            return;

        args.Handled = true;
    }

    #endregion

    #region Take&Return

    public bool TryTakeCandy(Entity<Scp330BowlComponent> ent, EntityUid user)
    {
        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        if (container.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("scp330-bowl-empty"), ent, user);
            return false;
        }

        var item = _random.Pick(container.ContainedEntities);
        if (!TrySignCandy(ent, item, user))
            return false;

        if (!_hands.TryPickup(user, item))
            return false;

        if (ent.Comp.ThiefCounter[user] > ent.Comp.PunishmentAfter)
            ApplyPunishment(ent, user);

        return true;
    }

    public bool TryReturnCandy(Entity<Scp330BowlComponent> ent, EntityUid item)
    {
        if (!_whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return false;

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        if (!_container.Insert(item, container))
            return false;

        // Не уверен, что false при уменьшении счетчика воровства стоит считать как false для всего метода
        // Поэтому посидит без него.
        TryDecreaseThiefCounter(ent, item);

        return true;
    }

    private bool TrySignCandy(Entity<Scp330BowlComponent> ent, Entity<Scp330CandyComponent?> candy, EntityUid user)
    {
        if (!Resolve(candy, ref candy.Comp, false))
            return false;

        candy.Comp.TakenBy = user;
        Dirty(candy);

        ent.Comp.ThiefCounter.TryAdd(user, 0);
        ent.Comp.ThiefCounter[user]++;
        Dirty(ent);

        return true;
    }

    private bool TryDecreaseThiefCounter(Entity<Scp330BowlComponent> ent, Entity<Scp330CandyComponent?> candy)
    {
        if (!Resolve(candy, ref candy.Comp, false))
            return false;

        if (!candy.Comp.TakenBy.HasValue)
            return false;

        if (!ent.Comp.ThiefCounter.TryGetValue(candy.Comp.TakenBy.Value, out var count))
            return false;

        if (count <= 0)
            return false;

        // Вернули конфету - уменьшаем счетчик "взятых конфет" для человека, который взял эту конфету.
        ent.Comp.ThiefCounter[candy.Comp.TakenBy.Value]--;
        Dirty(ent);

        return true;
    }

    #endregion

    #region Punishment

    private void ApplyPunishment(Entity<Scp330BowlComponent> ent, EntityUid user)
    {
        var extra = Math.Max(1, ent.Comp.ThiefCounter[user] - ent.Comp.PunishmentAfter);
        var radius = ent.Comp.BasePunishmentRadius + (extra * 0.5f);

        var bowlCoords = _transform.GetMapCoordinates(ent);
        _cachedEntities.Clear();
        _lookup.GetEntitiesInRange(bowlCoords, radius, _cachedEntities, LookupFlags.Dynamic);

        foreach (var target in _cachedEntities)
        {
            if (_mobState.IsIncapacitated(target))
                continue;

            // Проверяем на видимость. Подходят только цели без препятствий/за стеклом
            if (!_proximity.IsRightType(ent, target, LineOfSightBlockerLevel.Transparent))
                continue;

            var damage = CalculateDamage(target, bowlCoords, radius, in ent.Comp.BaseDamage);
            _damageable.TryChangeDamage(target, damage, true, origin: ent);
        }

        CutOffHands(user);
    }

    private void CutOffHands(EntityUid target)
    {
        if (!TryComp<BodyComponent>(target, out var body))
            return;

        var parts = _body.GetBodyChildren(target, body);
        _gibCachedEntities.Clear();

        foreach (var part in parts)
        {
            if (part.Component.PartType != BodyPartType.Hand)
                continue;

            // Отрезаем руку и бросаем под персонажа
            _gibbing.TryGibEntityWithRef(
                target,
                part.Id,
                GibType.Drop,
                GibContentsOption.Drop,
                ref _gibCachedEntities);
        }

        if (_gibCachedEntities.Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("scp330-removed-hands"), target, target, PopupType.LargeCaution);
        }
    }

    #endregion

    #region Helpers

    private DamageSpecifier CalculateDamage(EntityUid target, MapCoordinates bowlCoords, float maxRange, in DamageSpecifier baseDamage)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        var distance = Vector2.Distance(bowlCoords.Position, targetCoords.Position);
        var falloff = Math.Clamp(1f - (distance / maxRange), 0f, 1f);

        var newDamage = new DamageSpecifier();
        foreach (var (type, value) in baseDamage.DamageDict)
        {
            newDamage.DamageDict[type] = value * falloff;
        }

        return newDamage;
    }

    #endregion
}
