using Content.Server._Scp.Scp106.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp106;

public sealed class ArtifactScp106TeleportSystem : EntitySystem
{
    [Dependency] private readonly Scp106System _scp106 = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactScp106TeleportComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactScp106TeleportComponent> ent, ref ArtifactActivatedEvent args)
    {
        switch (ent.Comp.Mode)
        {
            case ArtifactScp106TeleportMode.Random:
                if (_random.Prob(0.5f))
                    _scp106.SendToStation(ent);
                else
                    _scp106.SendToBackrooms(ent);

                break;
            case ArtifactScp106TeleportMode.Station:
                _scp106.SendToStation(ent);

                break;

            case ArtifactScp106TeleportMode.Dimension:
                _scp106.SendToBackrooms(ent);

                break;
        }
    }
}
