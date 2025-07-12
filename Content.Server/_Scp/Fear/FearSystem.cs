using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem : SharedFearSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier FearIncreaseSound =
        new SoundPathSpecifier("/Audio/_Scp/Effects/Fear/increase.ogg", AudioParams.Default.WithVolume(5f));
    private static readonly SoundSpecifier FearDecreaseSound =
        new SoundPathSpecifier("/Audio/_Scp/Effects/Fear/decrease.ogg", AudioParams.Default.WithVolume(-1f));

    private static readonly SoundSpecifier BreathingSound =
        new SoundPathSpecifier("/Audio/_Scp/Effects/Fear/breathing.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, FearStateChangedEvent>(OnFearStateChanged);
        SubscribeLocalEvent<FearActiveSoundEffectsComponent, ComponentShutdown>(OnShutdown);

        InitializeFears();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FearComponent>();

        // Проходимся по людям с компонентом страха и уменьшаем уровень страха со временем
        while (query.MoveNext(out var uid, out var fear))
        {
            if (fear.State == FearState.None)
                continue;

            if (fear.NextTimeDecreaseFearLevel > _timing.CurTime)
                continue;

            var entity = (uid, fear);

            // Если по какой-то причине не получилось успокоиться, то ждем снова
            // Это нужно, чтобы игрок только что отойдя от источника страха не успокоился моментально
            if (!TryCalmDown(entity))
                SetNextCalmDownTime(entity);
        }

        UpdateHemophobia();
    }

    /// <summary>
    /// Проигрывает специфический звук в зависимости от установленного уровня страха.
    /// Для повышения и понижения уровня звуки разные.
    /// </summary>
    private void OnFearStateChanged(Entity<FearComponent> ent, ref FearStateChangedEvent args)
    {
        // Выбираем звук. Если уровень страха повысился, то проигрываем звук увеличения и наоборот.
        var sound = ent.Comp.State > args.OldState ? FearIncreaseSound : FearDecreaseSound;
        _audio.PlayGlobal(sound, ent);
    }

    private void OnShutdown(Entity<FearActiveSoundEffectsComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.BreathingAudioStream = _audio.Stop(ent.Comp.BreathingAudioStream);
    }

    protected override void StartBreathing(Entity<FearActiveSoundEffectsComponent> ent)
    {
        base.StartBreathing(ent);

        if (!ent.Comp.PlayBreathingSound)
            return;

        if (ent.Comp.BreathingAudioStream.HasValue)
            return;

        var audioParams = AudioParams.Default
            .AddVolume(ent.Comp.AdditionalVolume)
            .WithLoop(true);

        var audio = _audio.PlayGlobal(BreathingSound, ent, audioParams);
        ent.Comp.BreathingAudioStream = audio?.Entity;
    }
}
