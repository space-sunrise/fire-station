using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Scp.Scp939;
using Content.Shared.Flash;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeVisibility()
    {
        SubscribeLocalEvent<MobStateComponent, ComponentStartup>(OnMobStartup);

        SubscribeLocalEvent<Scp939VisibilityComponent, EntitySpokeEvent>(OnTargetSpoke);
        SubscribeLocalEvent<Scp939VisibilityComponent, EmoteEvent>(OnTargetEmote);
        SubscribeLocalEvent<Scp939VisibilityComponent, DownedEvent>(OnDown);
        SubscribeLocalEvent<ItemComponent, GunShotEvent>(OnShot);

        SubscribeLocalEvent<Scp939Component, AfterFlashedEvent>(OnFlash);
    }

    private void OnFlash(Entity<Scp939Component> ent, ref AfterFlashedEvent args)
    {
        ent.Comp.PoorEyesight = true;
        ent.Comp.PoorEyesightTimeStart = _timing.CurTime;

        var message = Loc.GetString("scp939-flashed", ("time", ent.Comp.PoorEyesightTime));
        _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);

        Dirty(ent);
    }

    private void OnTargetEmote(Entity<Scp939VisibilityComponent> ent, ref EmoteEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnDown(Entity<Scp939VisibilityComponent> ent, ref DownedEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnShot(Entity<ItemComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<Scp939VisibilityComponent>(args.User, out var scp939VisibilityComponent))
            return;

        MobDidSomething((args.User, scp939VisibilityComponent));
    }

    private void OnMobStartup(Entity<MobStateComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<Scp939Component>(ent))
            return;

        var visibilityComponent = EnsureComp<Scp939VisibilityComponent>(ent);
        visibilityComponent.VisibilityAcc = 0.001f;

        Dirty(ent, visibilityComponent);
    }

    private void OnTargetSpoke(Entity<Scp939VisibilityComponent> ent, ref EntitySpokeEvent args)
    {
        MobDidSomething(ent);
    }

    // TODO: Перенести на клиент?
    private void MobDidSomething(Entity<Scp939VisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = 0.001f;
        Dirty(ent);
    }
}
