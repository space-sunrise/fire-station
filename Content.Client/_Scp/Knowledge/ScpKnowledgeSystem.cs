using System.Diagnostics.CodeAnalysis;
using Content.Client.Examine;
using Content.Shared._Scp.Knowledge;
using Content.Shared._Scp.Knowledge.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Scp.Knowledge;

public sealed class ScpKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<EntProtoId, List<ProtoId<ScpKnowledgePrototype>>> _knowledgeByEntityPrototype = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RebuildCaches();
    }

    public bool TryGetPredictedExamineData(
        EntityUid target,
        out bool knowTarget,
        out string? nameOverride,
        out FormattedMessage? message)
    {
        knowTarget = true;
        nameOverride = null;
        message = null;

        if (!TryGetLocalKnowledgeState(out var knowledgeState) ||
            !TryGetKnowledgeIdsForEntity(target, out var knowledgeIds))
        {
            return false;
        }

        var requiresKnowledge = false;
        for (var i = 0; i < knowledgeIds.Count; i++)
        {
            var knowledgeId = knowledgeIds[i];
            var knowledge = _prototype.Index(knowledgeId);
            if (!knowledge.HideIdentityUntilKnown)
                continue;

            requiresKnowledge = true;
            if (!ScpKnowledgeLogic.WillBeKnownAfterExamine(knowledgeState, knowledgeId, knowledge))
                continue;

            nameOverride = Loc.GetString(knowledge.DisplayName);
            return true;
        }

        if (!requiresKnowledge)
            return false;

        knowTarget = false;
        message = new FormattedMessage();
        message.AddText(Loc.GetString("scp-knowledge-unknown-examine"));
        return true;
    }

    public void AddPredictedExamineVerbs(EntityUid user, EntityUid target, List<Verb> verbs)
    {
        if (_player.LocalEntity != user ||
            !TryComp(user, out ScpKnowledgeComponent? knowledgeState) ||
            !TryGetKnowledgeIdsForEntity(target, out var knowledgeIds))
        {
            return;
        }

        for (var i = 0; i < knowledgeIds.Count; i++)
        {
            var knowledgeId = knowledgeIds[i];
            var knowledge = _prototype.Index(knowledgeId);
            if (!ScpKnowledgeLogic.WillBeKnownAfterExamine(knowledgeState, knowledgeId, knowledge) ||
                knowledge.KnownExamineVerbText == null ||
                knowledge.KnownExamineText == null)
            {
                continue;
            }

            var examineMessage = new FormattedMessage();
            examineMessage.AddText(Loc.GetString(knowledge.KnownExamineText.Value));

            var examineVerb = new ExamineVerb
            {
                Act = () => _examine.SendExamineTooltip(user, target, examineMessage, false, false),
                Text = Loc.GetString(knowledge.KnownExamineVerbText.Value),
                Category = VerbCategory.Examine,
                Icon = new SpriteSpecifier.Texture(new(ExamineSystemShared.DefaultIconTexture)),
                ClientExclusive = true,
            };

            verbs.Add(examineVerb);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<ScpKnowledgePrototype>())
            return;

        RebuildCaches();
    }

    private bool TryGetLocalKnowledgeState([NotNullWhen(true)] out ScpKnowledgeComponent? knowledgeState)
    {
        knowledgeState = null;
        return _player.LocalEntity is { } localEntity && TryComp(localEntity, out knowledgeState);
    }

    private void RebuildCaches()
    {
        _knowledgeByEntityPrototype.Clear();

        foreach (var knowledge in _prototype.EnumeratePrototypes<ScpKnowledgePrototype>())
        {
            for (var i = 0; i < knowledge.EntityPrototypes.Count; i++)
            {
                var entityPrototype = knowledge.EntityPrototypes[i];
                if (!_knowledgeByEntityPrototype.TryGetValue(entityPrototype, out var knowledgeIds))
                {
                    knowledgeIds = [];
                    _knowledgeByEntityPrototype[entityPrototype] = knowledgeIds;
                }

                knowledgeIds.Add(knowledge.ID);
            }
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
}
