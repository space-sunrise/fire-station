using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Shared._Scp.Other.ScpPossibilities;

public sealed class SharedScpPossibilitiesSystem : EntitySystem
{
    [Dependency] IRobustRandom _random = default!;
    [Dependency] EntityWhitelistSystem _whitelist = default!;
    [Dependency] SharedPopupSystem _popup = default!;
    [Dependency] SharedEntityStorageSystem _entityStorage = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpPossibilitiesComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (ent.Comp.CanEjectPilotFromMech)
            TryEjectFromMech(ref args);

        if (ent.Comp.OpenContainer)
            TryOpenContainer(ent, ref args);
    }

    public void TryEjectFromMech(ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!TryComp<MechComponent>(target, out var mechComp))
                continue;

            if (mechComp.PilotSlot.ContainedEntity == null)
                continue;

            var ev = new MechEjectPilotEvent();
            RaiseLocalEvent(target, ev);
        }
    }

    public void TryOpenContainer(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!HasComp<EntityStorageComponent>(target))
                continue;

            if (!_whitelist.CheckBoth(target, ent.Comp.OpenContainerBlacklist, ent.Comp.OpenContainerWhitelist))
                continue;

            if (_random.Prob(ent.Comp.OpenContainerChance))
                if (_entityStorage.TryOpenStorage(ent, target, false, false))
                    continue;

            _popup.PopupClient(Loc.GetString("scp-possibilities-open-container-failed"), target, ent, PopupType.Medium);
        }
    }
}
