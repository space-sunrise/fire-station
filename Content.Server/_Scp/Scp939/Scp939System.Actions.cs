using Content.Server.Chat.Systems;
using Content.Server.Examine;
using Content.Shared._Scp.Scp939;
using Content.Shared._Scp.ScpMask;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Bed.Sleep;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId StatusEffect = "StatusEffectForcedSleeping";

    private void InitializeActions()
    {
        SubscribeLocalEvent<Scp939Component, Scp939SleepAction>(OnSleepAction);
        SubscribeLocalEvent<Scp939Component, Scp939GasAction>(OnGasAction);
        SubscribeLocalEvent<Scp939Component, Scp939MimicActionEvent>(OnMimic);

        SubscribeLocalEvent<MobStateComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnSleepAction(Entity<Scp939Component> ent, ref Scp939SleepAction args)
    {
        args.Handled = TrySleep(ent);
    }

    public bool TrySleep(Entity<Scp939Component> ent, float hibernationDuration = 0)
    {
        if (HasComp<SleepingComponent>(ent))
            return false;

        if (!_sleepingSystem.TrySleeping(ent.Owner))
            return false;

        hibernationDuration = hibernationDuration == 0 ? ent.Comp.HibernationDuration : hibernationDuration;
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffect, TimeSpan.FromSeconds(hibernationDuration));

        return true;
    }

    private void OnGasAction(Entity<Scp939Component> ent, ref Scp939GasAction args)
    {
        if (_scpMask.TryGetScpMask(ent, out var scpMask))
        {
            _scpMask.TryCreatePopup(ent, scpMask);
            return;
        }

        var xform = Transform(ent);
        var smokeEntity = Spawn(ent.Comp.SmokeProtoId, xform.Coordinates.SnapToGrid());

        _smokeSystem.StartSmoke(smokeEntity, ent.Comp.SmokeSolution, ent.Comp.SmokeDuration, ent.Comp.SmokeSpreadRadius);

        args.Handled = true;
    }

    private void OnMimic(Entity<Scp939Component> ent, ref Scp939MimicActionEvent args)
    {
        if (ent.Comp.RememberedMessages.Count == 0)
            return;

        var messagePair = _random.Pick(ent.Comp.RememberedMessages);

        if (TryComp<TTSComponent>(ent, out var ttsComponent))
        {
            ttsComponent.VoicePrototypeId = messagePair.Value.Value;
            Dirty(ent, ttsComponent);
        }

        _chat.TrySendInGameICMessage(ent,
            messagePair.Key,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal,
            nameOverride: messagePair.Value.Key,
            ignoreActionBlocker: true);

        args.Handled = true;
    }

    /// <summary>
    /// Запоминание последних сказанных возле 939 слов
    /// </summary>
    private void OnEntitySpoke(Entity<MobStateComponent> ent, ref EntitySpokeEvent args)
    {
        var query = _entityLookup.GetEntitiesInRange<Scp939Component>(Transform(ent).Coordinates, 16f);
        string? voicePrototype = null;

        if (TryComp<TTSComponent>(ent, out var ttsComponent))
            voicePrototype = ttsComponent.VoicePrototypeId;

        foreach (var scp in query)
        {
            if (!_examine.InRangeUnOccluded(ent, scp))
                continue;

            if (scp.Comp.RememberedMessages.Count >= scp.Comp.MaxRememberedMessages)
            {
                var randomKey = _random.Pick(scp.Comp.RememberedMessages.Keys);
                scp.Comp.RememberedMessages.Remove(randomKey);
            }

            var username = Identity.Name(ent, EntityManager);
            scp.Comp.RememberedMessages.TryAdd(args.Message, new(username, voicePrototype));
            Dirty(scp);
        }
    }
}
