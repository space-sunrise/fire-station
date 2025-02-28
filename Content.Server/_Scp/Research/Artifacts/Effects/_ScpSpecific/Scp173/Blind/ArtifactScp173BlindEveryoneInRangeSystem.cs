using Content.Server._Scp.Scp173;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp173.Blind;

public sealed class ArtifactScp173BlindEveryoneInRangeSystem : EntitySystem
{
    [Dependency] private readonly Scp173System _scp173 = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactScp173BlindEveryoneInRangeComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactScp173BlindEveryoneInRangeComponent> ent, ref ArtifactActivatedEvent args)
    {
        _scp173.BlindEveryoneInRange(ent, ent.Comp.Radius, ent.Comp.Time);
    }
}
