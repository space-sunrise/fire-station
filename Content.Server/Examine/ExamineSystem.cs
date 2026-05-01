using System.Linq;
using Content.Server._Scp.Knowledge; // Fire added - knowledge-gated SCP examine responses
using Content.Server.Verbs;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Examine
{
    [UsedImplicitly]
    public sealed class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private readonly ScpKnowledgeSystem _scpKnowledge = default!; // Fire added - filter SCP identity and known examine name overrides
        [Dependency] private readonly VerbSystem _verbSystem = default!;

        private readonly FormattedMessage _entityNotFoundMessage = new();
        private readonly FormattedMessage _entityOutOfRangeMessage = new();

        public override void Initialize()
        {
            base.Initialize();
            _entityNotFoundMessage.AddText(Loc.GetString("examine-system-entity-does-not-exist"));
            _entityOutOfRangeMessage.AddText(Loc.GetString("examine-system-cant-see-entity"));

            SubscribeNetworkEvent<ExamineSystemMessages.RequestExamineInfoMessage>(ExamineInfoRequest);
        }

        public override void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor)
        {
            if (!TryComp<ActorComponent>(player, out var actor))
                return;

            var session = actor.PlayerSession;

            SortedSet<Verb>? verbs = null;
            if (getVerbs)
                verbs = _verbSystem.GetLocalVerbs(target, player, typeof(ExamineVerb));

            var ev = new ExamineSystemMessages.ExamineInfoResponseMessage(
                GetNetEntity(target), 0, message, verbs?.ToList(), centerAtCursor
            );

            RaiseNetworkEvent(ev, session.Channel);
        }

        private void ExamineInfoRequest(ExamineSystemMessages.RequestExamineInfoMessage request, EntitySessionEventArgs eventArgs)
        {
            var player = eventArgs.SenderSession;
            var session = eventArgs.SenderSession;
            var channel = player.Channel;
            var entity = GetEntity(request.NetEntity);

            if (session.AttachedEntity is not {Valid: true} playerEnt
                || !Exists(entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.NetEntity, request.Id, _entityNotFoundMessage), channel);
                return;
            }

            if (!CanExamine(playerEnt, entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.NetEntity, request.Id, _entityOutOfRangeMessage, knowTarget: false), channel);
                return;
            }

            // Fire added start - still grant SCP knowledge from detail examine before hidden identity short-circuit
            _scpKnowledge.TryGrantExamineKnowledge(playerEnt, entity);
            // Fire added end

            // Fire added start - hide SCP entity identity until the mind learns it
            if (_scpKnowledge.TryGetUnknownExamineMessage(playerEnt, entity, out var unknownExamineMessage))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.NetEntity,
                    request.Id,
                    unknownExamineMessage,
                    knowTarget: false), channel);
                return;
            }
            // Fire added end

            SortedSet<Verb>? verbs = null;
            if (request.GetVerbs)
                verbs = _verbSystem.GetLocalVerbs(entity, playerEnt, typeof(ExamineVerb));

            // Fire added start - attach knowledge-based name override for known SCP examine responses
            _scpKnowledge.TryGetKnownExamineNameOverride(playerEnt, entity, out var nameOverride);
            var text = GetExamineText(entity, player.AttachedEntity);
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                request.NetEntity, request.Id, text, verbs?.ToList(), nameOverride: nameOverride), channel);
            // Fire added end
        }
    }
}
