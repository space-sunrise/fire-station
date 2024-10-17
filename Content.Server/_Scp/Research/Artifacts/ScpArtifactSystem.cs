using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts;

public sealed class ScpArtifactSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRestrictionComponent, MapInitEvent>(AnomalizeScps);
    }

    #region Core

    private void AnomalizeScps(Entity<ScpRestrictionComponent> scp, ref MapInitEvent args)
    {
        if (!TryComp<ArtifactComponent>(scp, out var artifactComponent))
            return;

        _artifactsSystem.RandomizeArtifact(scp, artifactComponent);
    }

    #endregion
}
