using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Other.ScpPossibilities;
using Content.Shared.Storage.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server._Scp.Other.ScpPossibilities;

public sealed partial class ScpPossibilitiesSystem : SharedScpPossibilitiesSystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void OnMeleeHit(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        base.OnMeleeHit(ent, ref args);

        if (ent.Comp.OpenContainer)
            TryOpenContainer(ent, ref args);
    }

    public void TryOpenContainer(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!HasComp<EntityStorageComponent>(target))
                continue;

            if (_random.Prob(ent.Comp.OpenContainerChance))
                if (_entityStorage.TryOpenStorage(ent, target, false, false))
                    continue;

            _popup.PopupEntity(Loc.GetString("scp-possibilities-open-container-failed"), target, ent, Shared.Popups.PopupType.Medium);
        }
    }
}
