using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._Scp.Proximity;
using Content.Shared.Body.Part;
using Content.Shared.Gibbing.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Gibbing.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Scp.Scp330;

public sealed class Scp330System : EntitySystem
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

    private readonly HashSet<Entity<HandsComponent>> _cachedEntities = [];
    private HashSet<EntityUid> _gibCachedEntities = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp330Component, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<Scp330Component> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        // если конфет нет
        if (ent.Comp.CurrentCandies <= 0)
        {
            _popup.PopupEntity("Миска пуста...", ent, args.User);
            return;
        }

        var candy = Spawn(ent.Comp.CandyPrototype, _transform.GetMapCoordinates(args.User));
        if (!_hands.TryPickupAnyHand(args.User, candy))
        {
            QueueDel(candy);
            return;
        }

        ent.Comp.CurrentCandies--;

        ent.Comp.ThiefCounter.TryAdd(args.User, 0);
        ent.Comp.ThiefCounter[args.User]++;

        if (ent.Comp.ThiefCounter[args.User] > ent.Comp.PunishmentAfter)
        {
            ApplyPunishment(ent, args.User);
        }

        args.Handled = true;
    }

    private void ApplyPunishment(Entity<Scp330Component> ent, EntityUid user)
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

    private DamageSpecifier CalculateDamage(EntityUid target, MapCoordinates bowlCoords, float maxRange, in DamageSpecifier baseDamage)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        var distance = Vector2.Distance(bowlCoords.Position, targetCoords.Position);
        var closePercent = Math.Clamp(distance / maxRange, 0f, 1f);

        var newDamage = new DamageSpecifier();
        foreach (var (type, value) in baseDamage.DamageDict)
        {
            newDamage.DamageDict[type] = value * closePercent;
        }

        return newDamage;
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
            _popup.PopupEntity("ВАШИ КИСТИ ОТВАЛИЛИСЬ!", target, target, PopupType.LargeCaution);
        }
    }
}
