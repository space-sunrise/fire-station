using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Part;
using Robust.Server.GameObjects;

namespace Content.Server._Scp.Scp330;

public sealed class Scp330System : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;

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
        var extra = ent.Comp.ThiefCounter[user] - ent.Comp.PunishmentAfter;
        var radius = 1.0f + (extra * 0.5f);
        ent.Comp.BaseDamage * 2f;

        // удаляет кисти и спавнит их на полу
        ManualCutOff(user);

        // цйущвуцацукщаукп
        var bowlCoords = _transform.GetMapCoordinates(ent);
        foreach (var entity in _lookup.GetEntitiesInRange(bowlCoords, radius))
        {
            if (!HasComp<DamageableComponent>(entity) || !HasComp<BodyComponent>(entity))
                continue;

            if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState != MobState.Alive)
                continue;

            _damageable.TryChangeDamage(entity, ent.Comp.BaseDamage, true);
        }
    }

    private void ManualCutOff(EntityUid target)
    {
        if (!TryComp<BodyComponent>(target, out var body))
            return;

        var coords = _transform.GetMapCoordinates(target);
        var parts = _body.GetBodyChildren(target, body);
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
}
