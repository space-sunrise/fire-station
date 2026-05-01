using Content.Shared._Scp.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Knowledge;

public sealed class ScpKnowledgeTextAnalysis(
    ScpKnowledgeTokenizedText tokenizedText,
    IReadOnlyList<ScpKnowledgeAnalyzedMatch> matches,
    IReadOnlyList<ProtoId<ScpKnowledgePrototype>> matchedKnowledgeIds)
{
    public static readonly ScpKnowledgeTextAnalysis Empty = new(
        ScpKnowledgeTokenizedText.Empty,
        [],
        []);

    public ScpKnowledgeTokenizedText TokenizedText { get; } = tokenizedText;
    public IReadOnlyList<ScpKnowledgeAnalyzedMatch> Matches { get; } = matches;
    public IReadOnlyList<ProtoId<ScpKnowledgePrototype>> MatchedKnowledgeIds { get; } = matchedKnowledgeIds;

    public bool HasMatches => Matches.Count > 0;
    public bool HasMatchedKnowledge => MatchedKnowledgeIds.Count > 0;
}

public readonly record struct ScpKnowledgeAnalyzedMatch(
    ProtoId<ScpKnowledgePrototype> KnowledgeId,
    int Start,
    int Length)
{
    public int End => Start + Length;
}
