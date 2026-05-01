#pragma warning disable IDE0130 // Namespace does not match folder structure
using Content.Server._Scp.Knowledge;
using Content.Shared._Scp.Helpers;
using Content.Shared.Chat;
using Content.Shared._Scp.Knowledge;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly ScpKnowledgeSystem _knowledge = default!;

    private ScpKnowledgeTextAnalysis? CreateScpKnowledgeTextAnalysis(string message)
    {
        var analysis = _knowledge.AnalyzeRecognitionText(message);
        return analysis.HasMatchedKnowledge ? analysis : null;
    }

    private void RaiseScpKnowledgeSpeakEvent(EntityUid source, ScpKnowledgeTextAnalysis? analysis, ChatTransmitRange range)
    {
        if (analysis == null)
            return;

        using var listeners = ListPool<EntityUid>.Rent();

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            if (session.AttachedEntity is not { Valid: true } attachedEntity)
                continue;

            if (MessageRangeCheck(session, data, range) == MessageRangeCheckResult.Disallowed)
                continue;

            listeners.Value.Add(attachedEntity);
        }

        RaiseScpKnowledgeSpeechEvent(source, analysis, listeners.Value);
    }

    private void RaiseScpKnowledgeWhisperEvent(
        EntityUid source,
        List<EntityUid> clearListeners,
        List<EntityUid> obfuscatedListeners,
        ScpKnowledgeTextAnalysis? clearAnalysis,
        ScpKnowledgeTextAnalysis? obfuscatedAnalysis)
    {
        RaiseScpKnowledgeSpeechEvent(source, clearAnalysis, clearListeners);
        RaiseScpKnowledgeSpeechEvent(source, obfuscatedAnalysis, obfuscatedListeners);
    }

    private void RaiseScpKnowledgeSpeechEvent(
        EntityUid source,
        ScpKnowledgeTextAnalysis? analysis,
        List<EntityUid> listeners)
    {
        if (analysis == null || listeners.Count == 0)
            return;

        var ev = new ScpKnowledgeSpeechHeardEvent(source, analysis, listeners.ToArray());
        RaiseLocalEvent(ref ev);
    }

    private string GetScpKnowledgeWrappedMessage(
        EntityUid listener,
        EntityUid source,
        string message,
        string wrappedMessage,
        ScpKnowledgeTextAnalysis? analysis = null)
    {
        return _knowledge.HighlightWrappedChatMessage(listener, message, wrappedMessage, source, analysis);
    }
}
