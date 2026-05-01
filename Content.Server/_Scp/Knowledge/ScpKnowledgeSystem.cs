using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Examine;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Scp.Knowledge;
using Content.Shared._Scp.Knowledge.Components;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Paper;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Knowledge;

public sealed partial class ScpKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<EntProtoId, List<ProtoId<ScpKnowledgePrototype>>> _knowledgeByEntityPrototype = [];
    private readonly Dictionary<string, List<CachedRecognitionPattern>> _knowledgePatternsByFirstToken = [];
    private readonly Dictionary<EntityUid, CachedPaperAnalysis> _paperAnalysisCache = [];
    private readonly List<CachedKnowledgeMatch> _matchedKnowledgeBuffer = [];
    private readonly HashSet<ProtoId<ScpKnowledgePrototype>> _matchedKnowledgeIdsBuffer = [];
    private readonly List<ScpKnowledgeTextMatch> _recognitionMatchesBuffer = [];
    private readonly ScpKnowledgePatternMatchContext _patternMatchContext = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<ScpKnowledgePaperReadEvent>(OnPaperRead);
        SubscribeLocalEvent<PaperComponent, AfterActivatableUIOpenEvent>(OnPaperUiOpened);
        SubscribeLocalEvent<PaperComponent, ComponentShutdown>(OnPaperShutdown);
        SubscribeLocalEvent<MetaDataComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MetaDataComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        SubscribeLocalEvent<ScpKnowledgeSpeechHeardEvent>(OnSpeechHeard);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioSpoke);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);

        RebuildCaches();
    }

    public bool TryGetKnowledgeState(
        EntityUid uid,
        [NotNullWhen(true)] out EntityUid? holderUid,
        [NotNullWhen(true)] out ScpKnowledgeComponent? knowledge)
    {
        if (TryComp(uid, out knowledge))
        {
            holderUid = uid;
            return true;
        }

        if (TryComp<MindComponent>(uid, out var mind) &&
            mind.CurrentEntity is { } currentEntity &&
            TryComp(currentEntity, out knowledge))
        {
            holderUid = currentEntity;
            return true;
        }

        if (!_mind.TryGetMind(uid, out var resolvedMindUid, out _))
        {
            holderUid = null;
            knowledge = null;
            return false;
        }

        if (TryComp(resolvedMindUid, out knowledge))
        {
            holderUid = resolvedMindUid;
            return true;
        }

        if (TryComp<MindComponent>(resolvedMindUid, out var resolvedMind) &&
            resolvedMind.CurrentEntity is { } resolvedCurrentEntity &&
            TryComp(resolvedCurrentEntity, out knowledge))
        {
            holderUid = resolvedCurrentEntity;
            return true;
        }

        holderUid = null;
        knowledge = null;
        return false;
    }

    public bool HasKnowledge(EntityUid uid, ProtoId<ScpKnowledgePrototype> knowledgeId)
    {
        if (!TryGetKnowledgeState(uid, out _, out var knowledgeState))
            return false;

        var knowledge = _prototype.Index(knowledgeId);
        return ScpKnowledgeLogic.IsKnowledgeKnown(knowledgeState, knowledgeId, knowledge);
    }

    public bool TryGetKnowledgeProgress(EntityUid uid, ProtoId<ScpKnowledgePrototype> knowledgeId, out int progress)
    {
        progress = 0;

        if (!TryGetKnowledgeState(uid, out _, out var knowledge))
            return false;

        var knowledgePrototype = _prototype.Index(knowledgeId);
        progress = ScpKnowledgeLogic.GetKnowledgeProgress(knowledge, knowledgeId, knowledgePrototype);
        return progress > 0;
    }

    public bool TryGrantKnowledgeProgress(
        EntityUid uid,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        int progress,
        ScpKnowledgeAcquisitionChannel channel = ScpKnowledgeAcquisitionChannel.Other,
        EntityUid? source = null,
        string? normalizedMessage = null)
    {
        return TryGrantKnowledgeProgress(uid, knowledgeId, progress, channel, source, true, normalizedMessage);
    }

    public bool TryGetUnknownExamineMessage(
        EntityUid examiner,
        EntityUid target,
        [NotNullWhen(true)] out FormattedMessage? message)
    {
        message = null;

        if (!TryGetKnowledgeIdsForEntity(target, out var knowledgeIds))
            return false;

        var requiresKnowledge = false;

        foreach (var knowledgeId in knowledgeIds)
        {
            var knowledge = _prototype.Index(knowledgeId);
            if (!knowledge.HideIdentityUntilKnown)
                continue;

            requiresKnowledge = true;
            if (HasKnowledge(examiner, knowledgeId))
                return false;
        }

        if (!requiresKnowledge)
            return false;

        message = new FormattedMessage();
        message.AddText(Loc.GetString("scp-knowledge-unknown-examine"));
        return true;
    }

    public bool TryGetKnownExamineNameOverride(
        EntityUid examiner,
        EntityUid target,
        [NotNullWhen(true)] out string? nameOverride)
    {
        nameOverride = null;

        if (!TryGetKnowledgeIdsForEntity(target, out var knowledgeIds))
            return false;

        foreach (var knowledgeId in knowledgeIds)
        {
            var knowledge = _prototype.Index(knowledgeId);
            if (!knowledge.HideIdentityUntilKnown || !HasKnowledge(examiner, knowledgeId))
                continue;

            nameOverride = Loc.GetString(knowledge.DisplayName);
            return true;
        }

        return false;
    }

    public bool TryGrantExamineKnowledge(EntityUid examiner, EntityUid target)
    {
        if (!TryGetKnowledgeIdsForEntity(target, out var knowledgeIds))
            return false;

        var changed = false;

        foreach (var knowledgeId in knowledgeIds)
        {
            var knowledge = _prototype.Index(knowledgeId);
            if (knowledge.ExamineProgress <= 0)
                continue;

            changed |= TryGrantKnowledgeProgress(
                examiner,
                knowledgeId,
                knowledge.ExamineProgress,
                ScpKnowledgeAcquisitionChannel.Examine,
                target);
        }

        return changed;
    }

    private bool TryGrantKnowledgeProgress(
        EntityUid uid,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        int progress,
        ScpKnowledgeAcquisitionChannel channel,
        EntityUid? source,
        bool showUnlockFeedback,
        string? normalizedMessage)
    {
        if (progress <= 0)
            return false;

        if (!TryGetKnowledgeState(uid, out var holderUid, out var knowledgeState))
            return false;

        if (!TryComp<ScpKnowledgeAcquisitionComponent>(holderUid.Value, out var acquisition))
            return false;

        var knowledge = _prototype.Index(knowledgeId);

        if (!CanAcquireKnowledge(acquisition, knowledge, channel))
            return false;

        if (IsKnowledgeKnown(knowledgeState, knowledgeId, knowledge))
            return false;

        if (source != null &&
            channel is ScpKnowledgeAcquisitionChannel.Read or ScpKnowledgeAcquisitionChannel.Examine &&
            !knowledgeState.ProcessedSources.Add(new ScpKnowledgeSourceRecord(source.Value, knowledgeId, channel)))
        {
            return false;
        }

        if (source != null &&
            channel == ScpKnowledgeAcquisitionChannel.Listen &&
            !string.IsNullOrEmpty(normalizedMessage) &&
            !knowledgeState.ProcessedMessages.Add(new ScpKnowledgeMessageRecord(
                source.Value,
                knowledgeId,
                channel,
                normalizedMessage)))
        {
            return false;
        }

        var currentProgress = ScpKnowledgeLogic.GetKnowledgeProgress(knowledgeState, knowledgeId, knowledge);
        var currentExposureFlags = ScpKnowledgeLogic.GetKnowledgeExposureFlags(knowledgeState, knowledgeId);
        var updatedExposureFlags = currentExposureFlags;

        if (ScpKnowledgeLogic.RequiresTextAndExamine(knowledge) &&
            channel != ScpKnowledgeAcquisitionChannel.Other)
        {
            updatedExposureFlags |= ScpKnowledgeLogic.GetExposureFlags(channel);
            progress = ScpKnowledgeLogic.GetExposureProgress(updatedExposureFlags);
        }

        var clampedProgress = ScpKnowledgeLogic.RequiresTextAndExamine(knowledge) &&
                              channel != ScpKnowledgeAcquisitionChannel.Other
            ? Math.Min(knowledge.RequiredProgress, Math.Max(currentProgress, progress))
            : Math.Min(knowledge.RequiredProgress, currentProgress + progress);

        if (clampedProgress <= currentProgress &&
            updatedExposureFlags == currentExposureFlags)
        {
            return false;
        }

        if (updatedExposureFlags == ScpKnowledgeExposureFlags.None)
            knowledgeState.ExposureFlags.Remove(knowledgeId);
        else
            knowledgeState.ExposureFlags[knowledgeId] = updatedExposureFlags;

        if (clampedProgress < knowledge.RequiredProgress)
        {
            knowledgeState.Progress[knowledgeId] = clampedProgress;
            Dirty(holderUid.Value, knowledgeState);
            return true;
        }

        knowledgeState.Progress.Remove(knowledgeId);
        knowledgeState.ExposureFlags.Remove(knowledgeId);
        knowledgeState.KnownKnowledge.Add(knowledgeId);

        Dirty(holderUid.Value, knowledgeState);

        if (showUnlockFeedback)
        {
            var unlockEvent = new ScpKnowledgeUnlockedEvent(knowledgeId, channel, source);
            RaiseLocalEvent(holderUid.Value, ref unlockEvent, true);
            ShowKnowledgeUnlockedFeedback(holderUid.Value, acquisition, knowledge);
        }

        return true;
    }

    private bool CanAcquireKnowledge(
        ScpKnowledgeAcquisitionComponent acquisition,
        ScpKnowledgePrototype knowledge,
        ScpKnowledgeAcquisitionChannel channel)
    {
        return channel switch
        {
            ScpKnowledgeAcquisitionChannel.Listen => acquisition.CanLearnByListen && knowledge.AllowListen,
            ScpKnowledgeAcquisitionChannel.Read => acquisition.CanLearnByRead && knowledge.AllowRead,
            ScpKnowledgeAcquisitionChannel.Examine => acquisition.CanLearnByExamine && knowledge.AllowExamine,
            ScpKnowledgeAcquisitionChannel.Other => acquisition.CanLearnByOther,
            _ => false,
        };
    }

    private void RebuildCaches()
    {
        _knowledgeByEntityPrototype.Clear();
        _knowledgePatternsByFirstToken.Clear();
        _paperAnalysisCache.Clear();

        foreach (var knowledge in _prototype.EnumeratePrototypes<ScpKnowledgePrototype>())
        {
            CacheKnowledgePatterns(knowledge);
            CacheKnowledgeEntities(knowledge);
        }
    }

    private void CacheKnowledgePatterns(ScpKnowledgePrototype knowledge)
    {
        var compiledPatternCount = 0;
        foreach (var patternId in knowledge.RecognitionPatterns)
        {
            var localizedPattern = Loc.GetString(patternId);
            if (!ScpKnowledgeText.TryCompileRecognitionPattern(localizedPattern, out var compiledPattern, out var error))
            {
                Log.Error(
                    $"Failed to compile SCP knowledge recognition pattern '{patternId}' for knowledge '{knowledge.ID}': {error}");
                continue;
            }

            compiledPatternCount++;

            foreach (var firstToken in compiledPattern.FirstTokens)
            {
                if (!_knowledgePatternsByFirstToken.TryGetValue(firstToken, out var patterns))
                {
                    patterns = [];
                    _knowledgePatternsByFirstToken[firstToken] = patterns;
                }

                patterns.Add(new CachedRecognitionPattern(knowledge.ID, compiledPattern));
            }
        }

        if (compiledPatternCount == 0)
            Log.Error($"SCP knowledge '{knowledge.ID}' has no valid recognition patterns.");
    }

    private void CacheKnowledgeEntities(ScpKnowledgePrototype knowledge)
    {
        foreach (var entityPrototype in knowledge.EntityPrototypes)
        {
            if (!_knowledgeByEntityPrototype.TryGetValue(entityPrototype, out var knowledgeIds))
            {
                knowledgeIds = [];
                _knowledgeByEntityPrototype[entityPrototype] = knowledgeIds;
            }

            knowledgeIds.Add(knowledge.ID);
        }
    }

    private bool TryGetKnowledgeIdsForEntity(
        EntityUid target,
        [NotNullWhen(true)] out List<ProtoId<ScpKnowledgePrototype>>? knowledgeIds)
    {
        knowledgeIds = null;
        var prototypeId = Prototype(target)?.ID;
        return prototypeId != null && _knowledgeByEntityPrototype.TryGetValue(prototypeId, out knowledgeIds);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<ScpKnowledgePrototype>())
            return;

        RebuildCaches();
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (!TryGetKnowledgeState(args.Entity, out var holderUid, out var knowledgeState))
            return;

        Dirty(holderUid.Value, knowledgeState);
    }

    private void OnPaperShutdown(Entity<PaperComponent> ent, ref ComponentShutdown args)
    {
        _paperAnalysisCache.Remove(ent.Owner);
    }

    private static ScpKnowledgeExposureFlags GetExposureFlags(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId)
    {
        return ScpKnowledgeLogic.GetKnowledgeExposureFlags(knowledgeState, knowledgeId);
    }

    private static bool IsKnowledgeKnown(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        ScpKnowledgePrototype knowledge)
    {
        return ScpKnowledgeLogic.IsKnowledgeKnown(knowledgeState, knowledgeId, knowledge);
    }

    private static int GetKnowledgeProgress(
        ScpKnowledgeComponent knowledgeState,
        ProtoId<ScpKnowledgePrototype> knowledgeId,
        ScpKnowledgePrototype knowledge)
    {
        return ScpKnowledgeLogic.GetKnowledgeProgress(knowledgeState, knowledgeId, knowledge);
    }

    public ScpKnowledgeTextAnalysis AnalyzeRecognitionText(string text, bool includeMatches = true)
    {
        return AnalyzeRecognitionText(ScpKnowledgeText.TokenizeRecognitionText(text), includeMatches);
    }

    public ScpKnowledgeTextAnalysis GetOrCreatePaperAnalysis(EntityUid paperUid, string content)
    {
        if (_paperAnalysisCache.TryGetValue(paperUid, out var cachedAnalysis) &&
            string.Equals(cachedAnalysis.Content, content, StringComparison.Ordinal))
        {
            return cachedAnalysis.Analysis;
        }

        var analysis = AnalyzeRecognitionText(content);
        _paperAnalysisCache[paperUid] = new CachedPaperAnalysis(content, analysis);
        return analysis;
    }

    private ScpKnowledgeTextAnalysis AnalyzeRecognitionText(
        ScpKnowledgeTokenizedText tokenizedText,
        bool includeMatches)
    {
        CollectKnowledgeMatches(tokenizedText, includeMatches);

        if (_matchedKnowledgeIdsBuffer.Count == 0)
            return new ScpKnowledgeTextAnalysis(tokenizedText, [], []);

        var matchedKnowledgeIds = new ProtoId<ScpKnowledgePrototype>[_matchedKnowledgeIdsBuffer.Count];
        _matchedKnowledgeIdsBuffer.CopyTo(matchedKnowledgeIds);
        if (!includeMatches || _matchedKnowledgeBuffer.Count == 0)
            return new ScpKnowledgeTextAnalysis(tokenizedText, [], matchedKnowledgeIds);

        var matches = new ScpKnowledgeAnalyzedMatch[_matchedKnowledgeBuffer.Count];
        var matchIndex = 0;
        foreach (var match in _matchedKnowledgeBuffer)
        {
            matches[matchIndex++] = new ScpKnowledgeAnalyzedMatch(match.KnowledgeId, match.Start, match.Length);
        }

        return new ScpKnowledgeTextAnalysis(tokenizedText, matches, matchedKnowledgeIds);
    }

    private void CollectKnowledgeMatches(ScpKnowledgeTokenizedText tokenizedText, bool includeMatches)
    {
        _matchedKnowledgeBuffer.Clear();
        _matchedKnowledgeIdsBuffer.Clear();

        if (tokenizedText.Tokens.Count == 0)
            return;

        for (var tokenIndex = 0; tokenIndex < tokenizedText.Tokens.Count; tokenIndex++)
        {
            var token = tokenizedText.Tokens[tokenIndex].Value;
            if (!_knowledgePatternsByFirstToken.TryGetValue(token, out var patterns))
                continue;

            foreach (var pattern in patterns)
            {
                _recognitionMatchesBuffer.Clear();
                ScpKnowledgeText.AddPatternMatchesAt(pattern.Pattern, tokenizedText, tokenIndex, _recognitionMatchesBuffer, _patternMatchContext);

                foreach (var match in _recognitionMatchesBuffer)
                {
                    if (includeMatches)
                        _matchedKnowledgeBuffer.Add(new CachedKnowledgeMatch(pattern.KnowledgeId, match.SourceStart, match.Length));

                    _matchedKnowledgeIdsBuffer.Add(pattern.KnowledgeId);
                }
            }
        }
    }

    private readonly record struct CachedRecognitionPattern(
        ProtoId<ScpKnowledgePrototype> KnowledgeId,
        ScpKnowledgeCompiledPattern Pattern);

    private readonly record struct CachedPaperAnalysis(string Content, ScpKnowledgeTextAnalysis Analysis);

    private readonly record struct CachedKnowledgeMatch(
        ProtoId<ScpKnowledgePrototype> KnowledgeId,
        int Start,
        int Length)
    {
        public int End => Start + Length;
    }
}
