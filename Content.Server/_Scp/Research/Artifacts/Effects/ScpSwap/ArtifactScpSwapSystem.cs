using System.Linq;
using Content.Server._Scp.Helpers;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.ScpSwap;

public sealed class ArtifactScpSwapSystem : BaseXAESystem<ArtifactScpSwapComponent>
{
    [Dependency] private readonly TransformSystem _transform= default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnActivated(Entity<ArtifactScpSwapComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var humans = _scpHelpers.GetAll<HumanoidAppearanceComponent>().ToList();
        var scps = _scpHelpers.GetAll<ScpComponent, MobStateComponent>().ToList();

        var ent1 = _random.PickAndTake(humans);
        var xform1 = Transform(ent1);

        var ent2 = _random.PickAndTake(scps);
        var xform2 = Transform(ent2);

        _transform.SwapPositions((ent1, xform1), (ent2, xform2));
    }
}
