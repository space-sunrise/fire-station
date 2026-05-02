using Content.Server._Scp.Other.ScpSleep;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;
using Content.Shared._Scp.Other.ScpSleep;

namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp939.Sleep;

public sealed class ArtifactScp939SleepSystem : BaseXAESystem<ArtifactScp939SleepComponent>
{
    [Dependency] private readonly ScpSleepSystem _scpSleep = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnActivated(Entity<ArtifactScp939SleepComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!TryComp<ScpSleepComponent>(ent, out var scpSleepComponent))
            return;

        var time = _random.Next(ent.Comp.MinSleepTime, ent.Comp.MaxSleepTime);

        _scpSleep.TrySleep((ent, scpSleepComponent), time);
    }
}
