using System.Text;
using Content.Shared._Scp.Knowledge;
using Content.Shared._Scp.Knowledge.Components;
using Content.Shared.Mind;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Knowledge;

public sealed partial class ScpKnowledgeSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const string KnowledgeHighlightColor = "#8b0000";
    private readonly List<TextHighlightRange> _highlightRanges = [];

    public string HighlightUnknownKnowledgeText(EntityUid viewer, string text, EntityUid? source = null)
    {
        return BuildHighlightedText(viewer, text, escapeText: false, source, analysis: null);
    }

    public string HighlightWrappedChatMessage(
        EntityUid viewer,
        string message,
        string wrappedMessage,
        EntityUid? source = null,
        ScpKnowledgeTextAnalysis? analysis = null)
    {
        if (message.Length == 0 || wrappedMessage.Length == 0)
            return wrappedMessage;

        var escapedMessage = FormattedMessage.EscapeText(message);
        var highlightedEscapedMessage = BuildHighlightedText(viewer, message, escapeText: true, source, analysis: analysis);
        if (!string.Equals(highlightedEscapedMessage, escapedMessage, StringComparison.Ordinal))
            return ReplaceFirstOccurrence(wrappedMessage, escapedMessage, highlightedEscapedMessage);

        var highlightedMessage = BuildHighlightedText(viewer, message, escapeText: false, source, analysis: analysis);
        if (!string.Equals(highlightedMessage, message, StringComparison.Ordinal))
            return ReplaceFirstOccurrence(wrappedMessage, message, highlightedMessage);

        return wrappedMessage;
    }

    private void OnPaperUiOpened(Entity<PaperComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (ent.Comp.Content.Length == 0)
            return;

        var analysis = GetOrCreatePaperAnalysis(ent.Owner, ent.Comp.Content);
        var highlightedText = HighlightUnknownPaperKnowledgeText(args.User, ent, analysis);
        if (string.Equals(highlightedText, ent.Comp.Content, StringComparison.Ordinal))
            return;

        _ui.ServerSendUiMessage(
            ent.Owner,
            PaperComponent.PaperUiKey.Key,
            new PaperComponent.PaperKnowledgeHighlightMessage(ent.Comp.Content, highlightedText),
            args.User);
    }

    private void ShowKnowledgeUnlockedFeedback(
        EntityUid holderUid,
        ScpKnowledgeAcquisitionComponent acquisition,
        ScpKnowledgePrototype knowledge)
    {
        var popupTarget = holderUid;

        if (TryComp<MindComponent>(holderUid, out var mind) && mind.CurrentEntity is { } currentEntity)
            popupTarget = currentEntity;

        var popup = Loc.GetString(
            "scp-knowledge-unlocked-popup",
            ("knowledge", Loc.GetString(knowledge.DisplayName)));

        _popup.PopupEntity(popup, popupTarget, popupTarget, PopupType.Medium);

        if (acquisition.UnlockSound != null)
        {
            if (TryComp<ActorComponent>(popupTarget, out var actor))
                _audio.PlayGlobal(acquisition.UnlockSound, actor.PlayerSession);
            else
                _audio.PlayGlobal(acquisition.UnlockSound, popupTarget);
        }
    }

    private string HighlightUnknownPaperKnowledgeText(
        EntityUid viewer,
        Entity<PaperComponent> paper,
        ScpKnowledgeTextAnalysis analysis)
    {
        return BuildHighlightedText(
            viewer,
            paper.Comp.Content,
            escapeText: false,
            source: null,
            matchFilter: (knowledgeId, start, length) => DoesPaperRangeProvideKnowledge(paper.Comp, knowledgeId, start, length),
            analysis: analysis);
    }

    private string BuildHighlightedText(
        EntityUid viewer,
        string text,
        bool escapeText,
        EntityUid? source,
        Func<ProtoId<ScpKnowledgePrototype>, int, int, bool>? matchFilter = null,
        ScpKnowledgeTextAnalysis? analysis = null)
    {
        if (text.Length == 0)
            return text;

        if (!TryGetKnowledgeState(viewer, out _, out var knowledgeState))
            return escapeText ? FormattedMessage.EscapeText(text) : text;

        ScpKnowledgeComponent? sourceKnowledgeState = null;
        if (source != null && !TryGetKnowledgeState(source.Value, out _, out sourceKnowledgeState))
            return escapeText ? FormattedMessage.EscapeText(text) : text;

        analysis ??= AnalyzeRecognitionText(text);
        if (!analysis.HasMatches)
            return escapeText ? FormattedMessage.EscapeText(text) : text;

        _highlightRanges.Clear();

        foreach (var match in analysis.Matches)
        {
            var knowledge = _prototype.Index(match.KnowledgeId);
            if (IsKnowledgeKnown(knowledgeState, match.KnowledgeId, knowledge))
                continue;

            if (sourceKnowledgeState != null && !IsKnowledgeKnown(sourceKnowledgeState, match.KnowledgeId, knowledge))
                continue;

            if (matchFilter != null && !matchFilter(match.KnowledgeId, match.Start, match.Length))
                continue;

            _highlightRanges.Add(new TextHighlightRange(match.Start, match.End));
        }

        if (_highlightRanges.Count == 0)
            return escapeText ? FormattedMessage.EscapeText(text) : text;

        _highlightRanges.Sort(static (left, right) =>
        {
            var compare = left.Start.CompareTo(right.Start);
            return compare != 0 ? compare : right.End.CompareTo(left.End);
        });

        return RenderHighlightedText(text, escapeText);
    }

    private bool HasPaperKnowledgeMatch(
        PaperComponent paper,
        ScpKnowledgeTextAnalysis analysis,
        ProtoId<ScpKnowledgePrototype> knowledgeId)
    {
        foreach (var match in analysis.Matches)
        {
            if (match.KnowledgeId != knowledgeId)
                continue;

            if (DoesPaperRangeProvideKnowledge(paper, knowledgeId, match.Start, match.Length))
                return true;
        }

        return false;
    }

    private bool DoesPaperRangeProvideKnowledge(PaperComponent paper, ProtoId<ScpKnowledgePrototype> knowledgeId, int start, int length)
    {
        if (length <= 0)
            return false;

        var authorRanges = paper.KnowledgeAuthorRanges;
        if (authorRanges.Count == 0)
            return true;

        var end = start + length;
        var coveredUntil = start;

        foreach (var range in authorRanges)
        {
            if (range.End <= start)
                continue;

            if (range.Start >= end)
                break;

            if (range.Start > coveredUntil)
                return false;

            if (range.Author == null)
            {
                coveredUntil = Math.Max(coveredUntil, Math.Min(range.End, end));
                continue;
            }

            if (!HasKnowledge(range.Author.Value, knowledgeId))
                return false;

            coveredUntil = Math.Max(coveredUntil, Math.Min(range.End, end));
        }

        return coveredUntil >= end;
    }

    private string RenderHighlightedText(string text, bool escapeText)
    {
        var builder = new StringBuilder(text.Length + _highlightRanges.Count * 32);
        var currentIndex = 0;
        var currentRange = _highlightRanges[0];

        for (var i = 1; i < _highlightRanges.Count; i++)
        {
            var nextRange = _highlightRanges[i];
            if (nextRange.Start <= currentRange.End)
            {
                currentRange = new TextHighlightRange(currentRange.Start, Math.Max(currentRange.End, nextRange.End));
                continue;
            }

            AppendHighlightedRange(builder, text, ref currentIndex, currentRange, escapeText);
            currentRange = nextRange;
        }

        AppendHighlightedRange(builder, text, ref currentIndex, currentRange, escapeText);
        if (currentIndex < text.Length)
            AppendRenderedSegment(builder, text, currentIndex, text.Length - currentIndex, escapeText);

        return builder.ToString();
    }

    private static void AppendHighlightedRange(
        StringBuilder builder,
        string text,
        ref int currentIndex,
        TextHighlightRange range,
        bool escapeText)
    {
        if (range.Start > currentIndex)
            AppendRenderedSegment(builder, text, currentIndex, range.Start - currentIndex, escapeText);

        builder.Append("[color=");
        builder.Append(KnowledgeHighlightColor);
        builder.Append(']');
        AppendRenderedSegment(builder, text, range.Start, range.End - range.Start, escapeText);
        builder.Append("[/color]");
        currentIndex = range.End;
    }

    private static void AppendRenderedSegment(
        StringBuilder builder,
        string text,
        int start,
        int length,
        bool escapeText)
    {
        if (length <= 0)
            return;

        if (!escapeText)
        {
            builder.Append(text, start, length);
            return;
        }

        var end = start + length;
        foreach (var character in text.AsSpan(start, length))
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '[':
                    builder.Append("\\[");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
    }

    private static string ReplaceFirstOccurrence(string text, string oldValue, string newValue)
    {
        var index = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (index == -1)
            return text;

        return string.Concat(
            text.AsSpan(0, index),
            newValue,
            text.AsSpan(index + oldValue.Length));
    }

    private readonly record struct TextHighlightRange(int Start, int End);
}
