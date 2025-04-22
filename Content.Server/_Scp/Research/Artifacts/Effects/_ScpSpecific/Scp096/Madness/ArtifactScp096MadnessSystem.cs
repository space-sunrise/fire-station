using System.Linq;
using Content.Server._Scp.Helpers;
using Content.Server._Scp.Scp096;
using Content.Server.Examine;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.ScpMask;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp096.Madness;

public sealed class ArtifactScp096MadnessSystem : BaseXAESystem<ArtifactScp096MadnessComponent>
{
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly Scp096System _scp096 = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;

    protected override void OnActivated(Entity<ArtifactScp096MadnessComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_scpHelpers.TryGetFirst<Scp096Component>(out var scp096))
            return;

        var coords = Transform(ent).Coordinates;
        var targets = _lookup.GetEntitiesInRange<BlinkableComponent>(coords, ent.Comp.Radius)
            .Where(h => _examine.InRangeUnOccluded(ent, h, ent.Comp.Radius))
            .ToHashSet();

        var reducedTargets = _scpHelpers.GetPercentageOfHashSet(targets, ent.Comp.Percent);

        foreach (var target in reducedTargets)
        {
            if (!_scp096.TryAddTarget(scp096.Value, target, true, true))
                continue;

            // TODO: Пофиксить разрыв маски много раз
            _scpMask.TryTear(scp096.Value);
        }
    }
}
