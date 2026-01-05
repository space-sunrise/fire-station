using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Server.Body.Components; 
using Content.Shared.Body.Components; 
using Content.Server.Body.Systems; 
using Content.Shared.Body.Part;
using Robust.Server.GameObjects; 
using Robust.Shared.Map;

namespace Content.Server.Scp330;

public sealed class Scp330System : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp330Component, ActivateInWorldEvent>(OnActivate);
    }

private void OnActivate(EntityUid uid, Scp330Component component, ActivateInWorldEvent args)
{
    if (args.Handled)
        return;

    var user = args.User;

    // типо если вообще нет рук
    if (!TryComp<HandsComponent>(user, out var hands) || hands.Hands.Count == 0)
    {
        _popup.PopupEntity("Вам нечем брать конфеты!", uid, user);
        return;
    }

    // если конфет нет
    if (component.CurrentCandies <= 0)
    {
        _popup.PopupEntity("Миска пуста...", uid, user);
        return;
    }

    component.CurrentCandies--;
    var candy = Spawn("Scp330Candy", _transform.GetMapCoordinates(user));
    _hands.PickupOrDrop(user, candy);
    args.Handled = true;

    if (!component.ThiefCounter.ContainsKey(user))
        component.ThiefCounter[user] = 0;

    component.ThiefCounter[user]++;

    if (component.ThiefCounter[user] > 2)
    {
        ApplyPunishment(uid, user, component);
    }
}

    private void ApplyPunishment(EntityUid bowl, EntityUid user, Scp330Component component)
    {
        var extra = component.ThiefCounter[user] - 2;
        var radius = 1.0f + (extra * 0.5f);
        var damageValue = component.BaseDamage + (extra * 20f);
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Slash", damageValue);

        // удаляет кисти и спавнит их на полу
        ManualCutOff(user);

        // цйущвуцацукщаукп
        var bowlCoords = _transform.GetMapCoordinates(bowl);
        foreach (var entity in _lookup.GetEntitiesInRange(bowlCoords, radius))
        {
            if (!HasComp<DamageableComponent>(entity) || !HasComp<BodyComponent>(entity))
                continue;

            if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState != MobState.Alive)
                continue;

            _damageable.TryChangeDamage(entity, damage, true);
        }
    }

    private void ManualCutOff(EntityUid target)
    {
        if (!TryComp<BodyComponent>(target, out var body))
            return;

        var coords = _transform.GetMapCoordinates(target);
        var parts = _bodySystem.GetBodyChildren(target, body);
        bool deleted = false;

        foreach (var part in parts)
        {
            if (part.Component.PartType == BodyPartType.Hand)
            {
                // Просто удаляем кисть из мира
                EntityManager.DeleteEntity(part.Id);
                deleted = true;
            }
        }

        if (deleted)
        {
            // спавн дефолтные кисти на пол
            Spawn("LeftHandHuman", coords);
            Spawn("RightHandHuman", coords);
            
            _popup.PopupEntity("ВАШИ КИСТИ ОТВАЛИЛИСЬ!", target, target, PopupType.LargeCaution);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<Scp330Component>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentCandies >= comp.MaxCandies)
                continue;

            comp.Accumulator += frameTime;
            if (comp.Accumulator >= comp.RegenDelay)
            {
                comp.Accumulator = 0;
                comp.CurrentCandies++;
            }
        }
    }
}