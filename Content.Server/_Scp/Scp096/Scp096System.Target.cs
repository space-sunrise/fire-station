using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Audio;
using Robust.Server.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private static readonly ProtoId<AmbientMusicPrototype> TargetAmbience = "Scp096Target";

    private void InitializeTarget()
    {
        SubscribeLocalEvent<Scp096TargetComponent, FearCalmDownAttemptEvent>(OnFearCalmDown);
    }

    private static void OnFearCalmDown(Entity<Scp096TargetComponent> ent, ref FearCalmDownAttemptEvent args)
    {
        args.Cancel();
    }

    protected override void OnTargetStartup(Entity<Scp096TargetComponent> ent, ref ComponentStartup args)
    {
        base.OnTargetStartup(ent, ref args);

        _pvsOverride.AddGlobalOverride(ent);
        RaiseNetworkEvent(new NetworkAmbientMusicEvent(TargetAmbience), ent);
        _audio.PlayGlobal(ent.Comp.SeenSound, ent);

        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    protected override void OnTargetShutdown(Entity<Scp096TargetComponent> ent, ref ComponentShutdown args)
    {
        base.OnTargetShutdown(ent, ref args);

        _pvsOverride.RemoveGlobalOverride(ent);
        RaiseNetworkEvent(new NetworkAmbientMusicEventStop(), ent);

        _meta.RemoveFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }
}
