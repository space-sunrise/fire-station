using Content.Server._Scp.Objectives.Components;
using Content.Server.Objectives;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.Objectives.Systems;

public sealed class DestroyScpConditionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DestroyScpConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<DestroyScpConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<DestroyScpConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<DestroyScpConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        var scpList = new List<EntityUid>();

        var query = AllEntityQuery<ScpComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (uid == args.Mind.OwnedEntity) // на всякий случай, проверка на себя-же
                continue;

            if (!TryComp<MindContainerComponent>(uid, out var mind) ||
                !mind.HasMind) // Исключаем возможность выдачи цели на пустых SCP/предметные объекты
                continue;

            if (!_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out _)) // Невозможно уничтожить
                continue;

            if (comp.ObjectName == null) // Имя объекта должно быть указано в компоненте
                continue;

            scpList.Add(uid);
        }

        if (scpList.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        condition.Comp.Target = _random.Pick(scpList);
    }

    private void OnAfterAssign(Entity<DestroyScpConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        if (condition.Comp.Target == null)
            return;

        if (!TryComp<ScpComponent>(condition.Comp.Target.Value, out var scpComp) ||
            scpComp.ObjectName == null)
            return;

        var title = Loc.GetString("objective-destroy-scp-text", ("target", scpComp.ObjectName));
        var description = Loc.GetString("objective-destroy-scp-description", ("target", scpComp.ObjectName));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
    }

    private void OnGetProgress(Entity<DestroyScpConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(condition);
    }

    private float GetProgress(DestroyScpConditionComponent condition)
    {
        if (condition.Target == null)
            return 0f;

        var target = condition.Target.Value;

        if (!Exists(target) || _mobState.IsDead(target))
            return 1f;

        return 0f;
    }
}
