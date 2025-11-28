using System.Numerics;
using Content.Server._Scp.Blood;
using Content.Server._Scp.Other.BreakDoorOnCollide;
using Content.Shared._Scp.Blood;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Robust.Server.Containers;
using Robust.Server.GameStates;

namespace Content.Server._Scp.Scp096;

public sealed class Scp096System : SharedScp096System
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly BloodSplatterSystem _bloodSplatter = default!;

    private const float BloodAngle = 360f;
    private const float BloodRadians = (float)Math.PI * 2f;
    private static readonly Vector2 BloodDistance = new (3f, 20f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<Scp096Component> ent, ref MapInitEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.FaceContainer, out var container))
            return;

        var face = Spawn(ent.Comp.FaceProto);

        if (!_container.Insert(face, container, force: true))
        {
            Log.Error($"Failed to insert SCP-096's face {ToPrettyString(face)} into container {container.ID}");
            QueueDel(face);
        }

        ent.Comp.FaceEntity = face;
        Dirty(ent);

        var faceComp = Comp<Scp096FaceComponent>(face);
        faceComp.FaceOwner = ent;
        Dirty(face, faceComp);
    }

    protected override void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        base.AddTarget(scp, target);

        _pvsOverride.AddGlobalOverride(target);
    }

    protected override void RemoveTarget(Entity<Scp096Component?> scp, EntityUid target, bool removeComponent = true)
    {
        base.RemoveTarget(scp, target, removeComponent);

        _pvsOverride.RemoveGlobalOverride(target);
    }

    protected override void OnRageStart(Entity<ActiveScp096RageComponent> ent, ref ComponentStartup args)
    {
        base.OnRageStart(ent, ref args);

        if (!TryComp<BreakDoorOnCollideComponent>(ent, out var breakDoor))
            return;

        breakDoor.Enabled = true;
    }

    protected override void OnRageShutdown(Entity<ActiveScp096RageComponent> ent, ref ComponentShutdown args)
    {
        base.OnRageShutdown(ent, ref args);

        if (!TryComp<BreakDoorOnCollideComponent>(ent, out var breakDoor))
            return;

        breakDoor.Enabled = false;
    }

    protected override void SpawnBlood(Entity<BloodSplattererComponent?> ent)
    {
        base.SpawnBlood(ent);

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        _bloodSplatter.SpawnBloodParticles(ent!, ent, BloodAngle, BloodRadians, BloodDistance);
    }
}
