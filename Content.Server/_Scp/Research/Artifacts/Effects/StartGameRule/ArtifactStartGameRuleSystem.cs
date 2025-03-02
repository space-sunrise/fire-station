using Content.Server.GameTicking;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server._Scp.Research.Artifacts.Effects.StartGamerule;

public sealed class ArtifactStartGameRuleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactStartGameRuleComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactStartGameRuleComponent> ent, ref ArtifactActivatedEvent args)
    {
        foreach (var (rule, amount) in ent.Comp.Rules)
        {
            for (var i = 0; i < amount; i++)
            {
                _gameTicker.StartGameRule(rule);
            }
        }
    }
}
