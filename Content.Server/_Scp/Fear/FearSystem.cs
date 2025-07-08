using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Content.Shared._Sunrise.Heartbeat;
using Content.Shared.GameTicking;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Fear;

public sealed class FearSystem : SharedFearSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier BreathingSound =
        new SoundPathSpecifier("/Audio/_Scp/Effects/Fear/breathing.ogg");

    private static readonly SoundSpecifier HeartbeatSound =
        new SoundPathSpecifier("/Audio/_Sunrise/Effects/heartbeat.ogg", AudioParams.Default.WithVolume(-3f));

    private static readonly HashSet<ICommonSession> HeartBeatDisabledSessions = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<HeartbeatOptionsChangedEvent>(OnOptionsChanged);

        SubscribeLocalEvent<FearActiveSoundEffectsComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => HeartBeatDisabledSessions.Clear());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FearActiveSoundEffectsComponent>();

        while (query.MoveNext(out var uid, out var effects))
        {
            if (_timing.CurTime < effects.NextHeartbeatTime)
                continue;

            if (IsDisabledByClient(uid))
                continue;

            var audioParams = AudioParams.Default.WithPitchScale(effects.Pitch).AddVolume(effects.AdditionalVolume);
            _audio.PlayGlobal(HeartbeatSound, uid, audioParams);

            SetNextTime(effects);
        }
    }

    private void OnShutdown(Entity<FearActiveSoundEffectsComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.BreathingAudioStream = _audio.Stop(ent.Comp.BreathingAudioStream);
    }

    protected override void StartBreathing(Entity<FearActiveSoundEffectsComponent> ent)
    {
        base.StartBreathing(ent);

        if (ent.Comp.BreathingAudioStream.HasValue)
            return;

        var audioParams = AudioParams.Default
            .AddVolume(ent.Comp.AdditionalVolume)
            .WithLoop(true);

        var audio = _audio.PlayGlobal(BreathingSound, ent, audioParams);
        ent.Comp.BreathingAudioStream = audio?.Entity;

        Dirty(ent);
    }

    protected override void StartHeartBeat(Entity<FearActiveSoundEffectsComponent> ent)
    {
        base.StartHeartBeat(ent);

        SetNextTime(ent);
    }

    private bool IsDisabledByClient(EntityUid player)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return true;

        if (HeartBeatDisabledSessions.Contains(session))
            return true;

        return false;
    }

    private static void OnOptionsChanged(HeartbeatOptionsChangedEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            HeartBeatDisabledSessions.Remove(args.SenderSession);
        else
            HeartBeatDisabledSessions.Add(args.SenderSession);
    }

    /// <summary>
    /// Устанавливает время следующего удара сердца
    /// </summary>
    private void SetNextTime(FearActiveSoundEffectsComponent component)
    {
        component.NextHeartbeatTime = _timing.CurTime + component.NextHeartbeatCooldown;
    }
}
