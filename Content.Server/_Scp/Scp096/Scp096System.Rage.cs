using Content.Server._Scp.Other.BreakDoorOnCollide;
using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System
{
    private static readonly ProtoId<AmbientMusicPrototype> RageAmbience = "Scp096Rage";

    protected override void OnHeatingUpStart(Entity<ActiveScp096HeatingUpComponent> ent, ref ComponentStartup args)
    {
        base.OnHeatingUpStart(ent, ref args);

        RaiseNetworkEvent(new NetworkAmbientMusicEvent(RageAmbience), ent);
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
}
