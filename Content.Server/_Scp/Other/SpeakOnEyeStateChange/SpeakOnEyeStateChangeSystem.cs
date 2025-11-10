using Content.Server.Chat.Systems;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp173;
using Robust.Shared.Random;

namespace Content.Server._Scp.Other.SpeakOnEyeStateChange;

/// <summary>
/// Система, реализующая автоматическое проигрывание фразы при изменении состояния глаз.
/// Персонаж игрока будет выдавать заданные фразы при закрытии и открытии глаз.
/// </summary>
public sealed class SpeakOnEyeStateChangeSystem : EntitySystem
{
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnEyeStateChangeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpeakOnEyeStateChangeComponent, EntityClosedEyesEvent>(OnClosedEyes);
        SubscribeLocalEvent<SpeakOnEyeStateChangeComponent, EntityOpenedEyesEvent>(OnOpenedEyes);
    }

    private void OnMapInit(Entity<SpeakOnEyeStateChangeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.PhraseOnClose != null && ent.Comp.PhraseOnClose.Count != 0)
            ent.Comp.FavoriteClosePhrase = _random.Pick(ent.Comp.PhraseOnClose);

        if (ent.Comp.PhraseOnOpen != null && ent.Comp.PhraseOnOpen.Count != 0)
            ent.Comp.FavoriteOpenPhrase = _random.Pick(ent.Comp.PhraseOnOpen);
    }

    private void OnClosedEyes(Entity<SpeakOnEyeStateChangeComponent> ent, ref EntityClosedEyesEvent args)
    {
        if (ent.Comp.PhraseOnClose == null || ent.Comp.PhraseOnClose.Count == 0)
            return;

        if (!ShouldSpeak(ent))
            return;

        var message = _random.Prob(ent.Comp.FavoriteChance) ? ent.Comp.FavoriteClosePhrase : _random.Pick(ent.Comp.PhraseOnClose);
        _chat.TrySendInGameICMessage(ent, Loc.GetString(message), InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private void OnOpenedEyes(Entity<SpeakOnEyeStateChangeComponent> ent, ref EntityOpenedEyesEvent args)
    {
        if (ent.Comp.PhraseOnOpen == null || ent.Comp.PhraseOnOpen.Count == 0)
            return;

        if (!ShouldSpeak(ent))
            return;

        var message = _random.Prob(ent.Comp.FavoriteChance) ? ent.Comp.FavoriteOpenPhrase : _random.Pick(ent.Comp.PhraseOnOpen);
        _chat.TrySendInGameICMessage(ent, Loc.GetString(message), InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    /// <summary>
    /// Должен ли персонаж выдать фразу в соответствии с параметрами в компоненте?
    /// </summary>
    private bool ShouldSpeak(Entity<SpeakOnEyeStateChangeComponent> ent)
    {
        if (ent.Comp.SpeakOnlyInScp173Chamber && !_proximity.IsNearby<Scp173BlockStructureDamageComponent>(ent, SharedScp173System.ContainmentRoomSearchRadius))
            return false;

        if (ent.Comp.SpeakOnlyWhileScp173Nearby && !_proximity.IsNearby<Scp173Component>(ent, SharedScp173System.ContainmentRoomSearchRadius))
            return false;

        return true;
    }
}
