using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Examine;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Other.ScpRememberPhrase;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Robust.Shared.Random;
using Content.Shared._Scp.Other.ScpOnSoundVisibility;

namespace Content.Server._Scp.Other.ScpRememberPhrase;

public sealed class ScpRememberPhraseSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRememberPhraseComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ScpOnSoundVisibilityComponent, EntitySpokeEvent>(OnSpoke); // TODO: Переделать под другой какой-то общий компонент / ивент
        SubscribeLocalEvent<ScpRememberPhraseComponent, ScpRememberPhraseActionEvent>(OnMimic);
    }

    private void OnInit(Entity<ScpRememberPhraseComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent, ent.Comp.ActionProto);
    }

    private void OnSpoke(Entity<ScpOnSoundVisibilityComponent> ent, ref EntitySpokeEvent args)
    {
        TryRememberPhrase(ent, args.Message);
    }

    private void OnMimic(Entity<ScpRememberPhraseComponent> ent, ref ScpRememberPhraseActionEvent args)
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
    /// Запоминание последних сказанных в округе
    /// </summary>
    private void TryRememberPhrase(Entity<ScpOnSoundVisibilityComponent> ent, string message)
    {
        using var rememberSet = HashSetPoolEntity<ScpRememberPhraseComponent>.Rent();
        _entityLookup.GetEntitiesInRange(Transform(ent).Coordinates, 16f, rememberSet.Value, LookupFlags.Dynamic | LookupFlags.Approximate);
        if (rememberSet.Value.Count == 0)
            return;

        string? voicePrototype = null;

        if (TryComp<TTSComponent>(ent, out var ttsComponent))
            voicePrototype = ttsComponent.VoicePrototypeId;

        foreach (var rememberEnt in rememberSet.Value)
        {
            if (!_examine.InRangeUnOccluded(ent, rememberEnt))
                continue;

            if (rememberEnt.Comp.RememberedMessages.Count >= rememberEnt.Comp.MaxRememberedMessages)
            {
                var randomKey = _random.Pick(rememberEnt.Comp.RememberedMessages.Keys);
                rememberEnt.Comp.RememberedMessages.Remove(randomKey);
            }

            var username = Identity.Name(ent, EntityManager);
            rememberEnt.Comp.RememberedMessages.TryAdd(message, new(username, voicePrototype));
        }
    }
}
