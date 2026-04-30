using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Examine;
using Content.Shared._Scp.Other.ScpRememberPhrase;
using Content.Shared._Scp.Other.Events;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Robust.Shared.Random;
using Content.Shared.Speech;

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

        SubscribeLocalEvent<ScpRememberPhraseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScpRememberPhraseComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScpRememberPhraseComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ScpRememberPhraseComponent, ScpRememberPhraseActionEvent>(OnMimic);
    }

    private void OnMapInit(Entity<ScpRememberPhraseComponent> ent, ref MapInitEvent args)
    {
        var actionEnt = _actionsSystem.AddAction(ent, ent.Comp.ActionProto);
        ent.Comp.ActionEnt = actionEnt;
    }

    private void OnShutdown(Entity<ScpRememberPhraseComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
        ent.Comp.ActionEnt = null;
    }

    public void OnListen(Entity<ScpRememberPhraseComponent> ent, ref ListenEvent args)
    {
        TryRememberPhrase(ent, args.Source, args.Message);
    }

    public void OnMimic(Entity<ScpRememberPhraseComponent> ent, ref ScpRememberPhraseActionEvent args)
    {
        if (ent.Comp.RememberedMessages.Count == 0)
            return;

        var messagePair = _random.Pick(ent.Comp.RememberedMessages);

        if (TryComp<TTSComponent>(ent, out var ttsComponent))
        {
            ttsComponent.VoicePrototypeId = messagePair.TtsVoice;
            Dirty(ent, ttsComponent);
        }

        _chat.TrySendInGameICMessage(ent,
            messagePair.Message,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal,
            nameOverride: messagePair.SpeakerName,
            ignoreActionBlocker: true);

        args.Handled = true;
    }

    /// <summary>
    /// Запоминание последних сказанных в округе
    /// </summary>
    public void TryRememberPhrase(Entity<ScpRememberPhraseComponent> ent, EntityUid speaker, string message)
    {
        string? voicePrototype = null;

        if (ent.Owner == speaker)
            return;

        if (TryComp<TTSComponent>(speaker, out var ttsComponent))
            voicePrototype = ttsComponent.VoicePrototypeId;

        if (ent.Comp.RememberedMessages.Count >= ent.Comp.MaxRememberedMessages)
            ent.Comp.RememberedMessages.RemoveAt(0);

        var username = Identity.Name(speaker, EntityManager);
        ent.Comp.RememberedMessages.Add(new RememberedMessage
        {
            Message = message,
            SpeakerName = username,
            TtsVoice = voicePrototype
        });
    }
}
