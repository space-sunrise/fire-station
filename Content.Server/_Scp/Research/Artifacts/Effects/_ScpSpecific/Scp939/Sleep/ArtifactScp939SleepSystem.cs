using Content.Server._Scp.Scp939;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared._Scp.Scp939;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp939.Sleep;

public sealed class ArtifactScp939SleepSystem : EntitySystem
{
    [Dependency] private readonly Scp939System _scp939 = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactScp939SleepComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactScp939SleepComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (!TryComp<Scp939Component>(ent, out var scp939Component))
            return;

        var time = _random.NextFloat(ent.Comp.MinSleepTime, ent.Comp.MaxSleepTime);

        _scp939.TrySleep((ent, scp939Component), time);
    }
}
