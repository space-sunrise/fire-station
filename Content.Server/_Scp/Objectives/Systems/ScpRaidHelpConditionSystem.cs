using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server._Scp.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server._Scp.Objectives.Systems;

public sealed class ScpRaidHelpConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRaidHelpConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<ScpRaidHelpConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var raidQuery = EntityQueryEnumerator<ChaosRaidRuleComponent>();
        while (raidQuery.MoveNext(out _, out var raid))
        {
            args.Progress = raid.WinConditions.Contains(ChaosWinCondition.ChaosRaidersCompleteAllObjectives) ? 1f : 0f;
            return;
        }

        args.Progress = 0f;
    }
}
