using Content.Shared._Scp.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Knowledge;

public static class ScpKnowledgeLogic
{
    public static bool RequiresTextAndExamine(ScpKnowledgePrototype knowledge)
    {
        return knowledge.Kind == ScpKnowledgeKind.Entity;
    }

    public static bool IsTextChannel(ScpKnowledgeAcquisitionChannel channel)
    {
        return channel is ScpKnowledgeAcquisitionChannel.Listen or ScpKnowledgeAcquisitionChannel.Read;
    }

    public static ScpKnowledgeExposureFlags GetExposureFlags(ScpKnowledgeAcquisitionChannel channel)
    {
        return channel switch
        {
            ScpKnowledgeAcquisitionChannel.Listen => ScpKnowledgeExposureFlags.Text,
            ScpKnowledgeAcquisitionChannel.Read => ScpKnowledgeExposureFlags.Text,
            ScpKnowledgeAcquisitionChannel.Examine => ScpKnowledgeExposureFlags.Examine,
            _ => ScpKnowledgeExposureFlags.None,
        };
    }

    public static int GetExposureProgress(ScpKnowledgeExposureFlags flags)
    {
        var progress = 0;

        if ((flags & ScpKnowledgeExposureFlags.Text) != 0)
            progress += 1;

        if ((flags & ScpKnowledgeExposureFlags.Examine) != 0)
            progress += 1;

        return progress;
    }

    public static ScpKnowledgeExposureFlags GetKnowledgeExposureFlags(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId)
    {
        return knowledgeState.ExposureFlags.GetValueOrDefault(knowledgeId, ScpKnowledgeExposureFlags.None);
    }

    public static int GetKnowledgeProgress(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        ScpKnowledgePrototype knowledge)
    {
        if (knowledgeState.KnownKnowledge.Contains(knowledgeId))
            return knowledge.RequiredProgress;

        knowledgeState.Progress.TryGetValue(knowledgeId, out var progress);

        if (!RequiresTextAndExamine(knowledge))
            return progress;

        var exposureProgress = GetExposureProgress(GetKnowledgeExposureFlags(knowledgeState, knowledgeId));
        return Math.Max(progress, exposureProgress);
    }

    public static bool IsKnowledgeKnown(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        ScpKnowledgePrototype knowledge)
    {
        return knowledgeState.KnownKnowledge.Contains(knowledgeId) ||
               GetKnowledgeProgress(knowledgeState, knowledgeId, knowledge) >= knowledge.RequiredProgress;
    }

    public static bool WillBeKnownAfterExamine(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        ScpKnowledgePrototype knowledge)
    {
        if (IsKnowledgeKnown(knowledgeState, knowledgeId, knowledge))
            return true;

        if (!RequiresTextAndExamine(knowledge) ||
            !knowledge.AllowExamine ||
            knowledge.ExamineProgress <= 0)
        {
            return false;
        }

        var exposureFlags = GetKnowledgeExposureFlags(knowledgeState, knowledgeId) | ScpKnowledgeExposureFlags.Examine;
        var progress = Math.Min(
            knowledge.RequiredProgress,
            Math.Max(GetKnowledgeProgress(knowledgeState, knowledgeId, knowledge), GetExposureProgress(exposureFlags)));

        return progress >= knowledge.RequiredProgress;
    }
}
