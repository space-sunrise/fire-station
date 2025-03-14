using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Audio.Systems;

public sealed class EchoEffectSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<AudioPresetPrototype> EchoEffectPreset = "Bathroom";

    private static readonly TimeSpan RaceConditionWaiting = TimeSpan.FromTicks(10L);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentInit>(OnInit, before: [typeof(SharedAudioSystem)]);
    }

    private void OnInit(Entity<AudioComponent> ent, ref ComponentInit args)
    {
        ApplyEcho(ent);
    }

    public void ApplyEcho(Entity<AudioComponent> sound)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Exists(sound))
            return;

        // Фоновая музыка не должна подвергаться эффектам эха
        if (sound.Comp.Global)
            return;

        // ЕБАННЫЙ РОТ ЭТОГО РЕЙС КОДИШЕН
        /*
         Лонг-рид причина почему тут стоит таймер:
         Как только only server-side звук приходит сюда, он идет в мою крутую систему и там вызывает только серверную систему добавления Auxiliary
         Тот вызывает Dirty(), который отлавливается на клиенте вручную через компонент стейт
         Там на аудио сурс навешивается эффект. Только по умолчанию сурс это дамми(заглушка)
         Аудио сурс выставляется на подписке AudioComponent на ComponentStartup().
         Так как я могу подписаться только ComponentInit(), который идет раньше, чем ComponentStartup()
         То мой ивент происходит раньше, чем выставляется аудиосурс на клиенте -> сервер успевает втиснуться в промежуток между этой хуйней
         И добавить эффект на заглушку, которая ниче не сделает. Поэтому я на рандом поставил сюда 10 тиков, за это время все успевает сработать
         ГОВНО
         */
        Timer.Spawn(RaceConditionWaiting, () =>
        {
            _effectsManager.TryAddEffect(sound, EchoEffectPreset);
        });
    }
}
