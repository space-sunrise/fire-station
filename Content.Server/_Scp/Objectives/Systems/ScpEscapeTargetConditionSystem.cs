using Content.Server._Scp.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server._Scp.Objectives.Systems;

public sealed class ScpEscapeTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpEscapeTargetConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<ScpEscapeTargetConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(ent, out var target) ||
            !TryComp<MindComponent>(target, out var mind) ||
            mind.OwnedEntity == null ||
            _mind.IsCharacterDeadIc(mind))
        {
            args.Progress = 0f;
            return;
        }

        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
        {
            args.Progress = 0f;
            return;
        }

        var targetEscaping = _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value);
        if (_emergencyShuttle.ShuttlesLeft)
            args.Progress = targetEscaping ? 1f : 0f;
        else
            args.Progress = targetEscaping ? 0.5f : 0f;
    }
}
