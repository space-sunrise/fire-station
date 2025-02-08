using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared._Scp.Research.Artifacts;

namespace Content.Server._Scp.Research.Artifacts.Triggers.HealthAnalyzerInteraction;

public sealed class ArtifactHealthAnalyzerInteractionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactHealthAnalyzerInteractionTriggerComponent, EntityAnalyzedEvent>(OnInteract);
    }

    private void OnInteract(Entity<ArtifactHealthAnalyzerInteractionTriggerComponent> target, ref EntityAnalyzedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(target);
    }
}
