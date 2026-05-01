using Content.Server.Chat.Systems;
using Content.Shared._Scp.Knowledge;
using Content.Shared._Scp.Knowledge.Components;
using Content.Shared.Examine;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Knowledge;

public sealed partial class ScpKnowledgeSystem
{
    private void OnSpeechHeard(ref ScpKnowledgeSpeechHeardEvent args)
    {
        if (!args.Analysis.HasMatchedKnowledge)
            return;

        foreach (var listener in args.Listeners)
        {
            GrantKnowledgeFromText(listener, args.Analysis, args.Source, ScpKnowledgeAcquisitionChannel.Listen);
        }
    }

    private void OnRadioSpoke(RadioSpokeEvent args)
    {
        var analysis = AnalyzeRecognitionText(args.Message, includeMatches: false);
        if (!analysis.HasMatchedKnowledge)
            return;

        foreach (var receiver in args.Receivers)
        {
            GrantKnowledgeFromText(receiver, analysis, args.Source, ScpKnowledgeAcquisitionChannel.Listen);
        }
    }

    private void OnPaperRead(ScpKnowledgePaperReadEvent args)
    {
        if (!TryComp<PaperComponent>(args.Paper, out var paper))
            return;

        var analysis = GetOrCreatePaperAnalysis(args.Paper, args.Content);
        if (!analysis.HasMatchedKnowledge)
            return;

        foreach (var knowledgeId in analysis.MatchedKnowledgeIds)
        {
            if (!HasPaperKnowledgeMatch(paper, analysis, knowledgeId))
                continue;

            var knowledge = _prototype.Index(knowledgeId);
            if (knowledge.ReadProgress <= 0)
                continue;

            TryGrantKnowledgeProgress(
                args.User,
                knowledgeId,
                knowledge.ReadProgress,
                ScpKnowledgeAcquisitionChannel.Read,
                args.Paper);
        }
    }

    private void OnExamined(Entity<MetaDataComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        TryGrantExamineKnowledge(args.Examiner, ent.Owner);
    }

    private void OnGetExamineVerbs(Entity<MetaDataComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryGetKnowledgeIdsForEntity(ent.Owner, out var knowledgeIds))
            return;

        foreach (var knowledgeId in knowledgeIds)
        {
            if (!HasKnowledge(args.User, knowledgeId))
                continue;

            var knowledge = _prototype.Index(knowledgeId);
            if (knowledge.KnownExamineVerbText == null || knowledge.KnownExamineText == null)
                continue;

            var message = new FormattedMessage();
            message.AddText(Loc.GetString(knowledge.KnownExamineText.Value));

            _examine.AddDetailedExamineVerb(
                args,
                ent.Comp,
                message,
                Loc.GetString(knowledge.KnownExamineVerbText.Value));
        }
    }

    private void GrantKnowledgeFromText(
        EntityUid uid,
        ScpKnowledgeTextAnalysis analysis,
        EntityUid source,
        ScpKnowledgeAcquisitionChannel channel)
    {
        if (!analysis.HasMatchedKnowledge)
            return;

        ScpKnowledgeComponent? sourceKnowledgeState = null;
        if (channel == ScpKnowledgeAcquisitionChannel.Listen &&
            !TryGetKnowledgeState(source, out _, out sourceKnowledgeState))
        {
            return;
        }

        foreach (var knowledgeId in analysis.MatchedKnowledgeIds)
        {
            var knowledge = _prototype.Index(knowledgeId);
            if (channel == ScpKnowledgeAcquisitionChannel.Listen &&
                (sourceKnowledgeState == null || !IsKnowledgeKnown(sourceKnowledgeState, knowledgeId, knowledge)))
            {
                continue;
            }

            var progress = channel switch
            {
                ScpKnowledgeAcquisitionChannel.Listen => knowledge.ListenProgress,
                ScpKnowledgeAcquisitionChannel.Read => knowledge.ReadProgress,
                ScpKnowledgeAcquisitionChannel.Examine => knowledge.ExamineProgress,
                _ => 0,
            };

            if (progress <= 0)
                continue;

            TryGrantKnowledgeProgress(uid, knowledgeId, progress, channel, source, analysis.TokenizedText.NormalizedText);
        }
    }
}
