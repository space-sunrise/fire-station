using System.Linq;
using Content.Shared._RMC14.Xenonids.Screech;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Coordinates;
using Content.Shared.Popups;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedFearSystem _fear = default!;

    private const float TerrifyRange = 23f;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<Scp106Component, Scp106BackroomsAction>(OnBackroomsAction);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportAction>(OnRandomTeleportAction);
        SubscribeLocalEvent<Scp106Component, Scp106BecomePhantomAction>(OnScp106BecomePhantomAction);
        SubscribeLocalEvent<Scp106Component, Scp106BecomeTeleportPhantomAction>(OnBecomeTeleportPhantomAction);
        SubscribeLocalEvent<Scp106Component, Scp106TerrifyNearbyActionEvent>(OnTerrify);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsActionEvent>(OnBackroomsDoAfter);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportActionEvent>(OnTeleportDoAfter);
    }

    private void OnBackroomsAction(Entity<Scp106Component> ent, ref Scp106BackroomsAction args)
    {
        if (IsInDimension(ent))
        {
            _popup.PopupEntity("Вы уже в своем измерении", ent, ent, PopupType.SmallCaution);
            return;
        }

        TryDoTeleport(ent, ref args, new Scp106BackroomsActionEvent ());
    }

    private void OnRandomTeleportAction(Entity<Scp106Component> ent, ref Scp106RandomTeleportAction args)
    {
        TryDoTeleport(ent, ref args, new Scp106RandomTeleportActionEvent ());
    }

    private void OnScp106BecomePhantomAction(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args)
    {
        if (args.Handled)
            return;

        if (!TryDeductEssence(ent, args.Cost))
            return;

        BecomePhantom(ent, ref args);
    }

    private void OnBecomeTeleportPhantomAction(Entity<Scp106Component> ent, ref Scp106BecomeTeleportPhantomAction args)
    {
        if (IsContained(ent))
            return;

        if (!TryDeductEssence(ent, args.Cost))
            return;

        BecomeTeleportPhantom(ent, ref args);
    }

    private void OnBackroomsDoAfter(Entity<Scp106Component> ent, ref Scp106BackroomsActionEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        DoTeleportEffects(ent);
        _ = SendToBackrooms(ent);

        args.Handled = true;
    }

    private void OnTeleportDoAfter(Entity<Scp106Component> ent, ref Scp106RandomTeleportActionEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        DoTeleportEffects(ent);
        SendToStation(ent);

        args.Handled = true;
    }

    private void OnTerrify(Entity<Scp106Component> ent, ref Scp106TerrifyNearbyActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (IsContained(ent))
            return;

        if (ent.Comp.AbsorbedFears.Count == 0)
            return;

        if (!TryDeductEssence(ent, args.Cost))
            return;

        var nearby = _lookup.GetEntitiesInRange<FearComponent>(Transform(ent).Coordinates, TerrifyRange);
        var state = ent.Comp.AbsorbedFears.Max();

        foreach (var target in nearby)
        {
            _fear.TrySetFearLevel(target.AsNullable(), state);
        }

        ent.Comp.AbsorbedFears.Remove(state);
        Dirty(ent);
        StartScreech(ent.Owner);

        args.Handled = true;
    }

    private void StartScreech(Entity<XenoScreechComponent?> ent, bool playSound = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (playSound)
            _audio.PlayPvs(ent.Comp.Sound, ent);

        SpawnAttachedTo(ent.Comp.Effect, ent.Owner.ToCoordinates());
    }
}
