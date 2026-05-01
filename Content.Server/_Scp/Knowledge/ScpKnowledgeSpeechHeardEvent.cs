namespace Content.Server._Scp.Knowledge;

[ByRefEvent]
public readonly record struct ScpKnowledgeSpeechHeardEvent(
    EntityUid Source,
    ScpKnowledgeTextAnalysis Analysis,
    EntityUid[] Listeners);
