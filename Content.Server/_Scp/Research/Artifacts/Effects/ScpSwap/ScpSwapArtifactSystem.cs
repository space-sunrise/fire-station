using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Humanoid;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.ScpSwap;

public sealed class ScpSwapArtifactSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpSwapArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ScpSwapArtifactComponent component, ArtifactActivatedEvent args)
    {
        var humans = EntityQuery<HumanoidAppearanceComponent>().ToList();
        var scps = EntityQuery<ScpComponent>().ToList();

        var ent1 = _random.PickAndTake(humans).Owner;
        var xform1 = Transform(ent1);

        var ent2 = _random.PickAndTake(scps).Owner;
        var xform2 = Transform(ent2);

        _xform.SwapPositions((ent1, xform1), (ent2, xform2));
    }
}
