
using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._Scp.GameRules.ScpSl;

public partial class ScpSlGameRuleSystem
{
    private void InitializeTracker()
    {
        SubscribeLocalEvent<ScpSlHumanoidMarkerComponent, MobStateChangedEvent>(OnHumanoidMobStateChanged);
        SubscribeLocalEvent<ScpSlHumanoidMarkerComponent, ComponentRemove>(OnHumanoidRemoved);

        SubscribeLocalEvent<ScpSlScpMarkerComponent, MobStateChangedEvent>(OnScpMobStateChanged);
        SubscribeLocalEvent<ScpSlScpMarkerComponent, ComponentRemove>(OnScpRemoved);
    }

    private void OnScpMobStateChanged(Entity<ScpSlScpMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryGetActiveRule(out var rule))
        {
            return;
        }

        if (args.NewMobState != MobState.Alive)
        {
            ent.Comp.Contained = true;
            TryEndRound();

            _ghostSystem.SpawnGhost(ent.Owner, canReturn: false);
        }
    }

    private void OnScpRemoved(Entity<ScpSlScpMarkerComponent> ent, ref ComponentRemove args)
    {
        TryEndRound();
    }


    private void OnHumanoidMobStateChanged(Entity<ScpSlHumanoidMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            TryEndRound();
        }
    }

    private void OnHumanoidRemoved(Entity<ScpSlHumanoidMarkerComponent> ent, ref ComponentRemove args)
    {
        TryEndRound();
    }
}

