using Content.Shared._Scp.Audio;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio.Echo;
/// <summary>
/// Система, накладывающая эффект эхо каждому неглобальному звуку.
/// Эффект может быть отключен игроком в настройках
/// </summary>
public sealed class EchoEffectSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly ProtoId<AudioPresetPrototype> StandardEchoEffectPreset = "Bathroom";
    private static readonly ProtoId<AudioPresetPrototype> StrongEchoEffectPreset = "SewerPipe";

    private bool _isClientSideEnabled;
    private bool _strongPresetPreferred;

    private EntityQuery<AudioComponent> _audioQuery;
    private EntityQuery<AudioEchoEffectAffectedComponent> _echoQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioEffectedComponent, ComponentStartup>(OnEffectedAudioStartup, after: [typeof(SharedAudioSystem)]);

        Subs.CVar(_cfg, ScpCCVars.EchoEnabled, OnEnabledToggled, true);
        Subs.CVar(_cfg, ScpCCVars.EchoStrongPresetPreferred, OnPreferredPresetToggled, true);

        _audioQuery = GetEntityQuery<AudioComponent>();
        _echoQuery = GetEntityQuery<AudioEchoEffectAffectedComponent>();
    }

    private void OnEffectedAudioStartup(Entity<AudioEffectedComponent> ent, ref ComponentStartup args)
    {
        if (!_isClientSideEnabled)
            return;

        if (!_audioQuery.TryComp(ent.Owner, out var audio))
            return;

        TryApplyEcho((ent.Owner, audio));
    }

    /// <summary>
    /// Пытается применить эхо к данном звуку
    /// </summary>
    /// <param name="sound">Звук, к которому будет применен эффект</param>
    /// <param name="preset">Пресет, если нужно выставить какой-то особенный</param>
    /// <returns>Получилось или не получилось применить эффект</returns>
    public bool TryApplyEcho(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype>? preset = null)
    {
        if (TerminatingOrDeleted(sound))
            return false;

        // Выбираем пресет для эха исходя из настроек игрока и возможного приоритетного эффекта при вызове извне системы
        var clientPreferredPreset = _strongPresetPreferred ? StrongEchoEffectPreset : StandardEchoEffectPreset;
        var targetPreset = preset ?? clientPreferredPreset;

        _effectsManager.TryAddEffect(sound, targetPreset);

        // Добавляем компонент-маркер к звуку, который будет хранить эффект эха
        var echoComp = AddComp<AudioEchoEffectAffectedComponent>(sound);
        echoComp.Preset = targetPreset;

        return true;
    }

    /// <summary>
    /// Пытается убрать эффект эхо у выбранного звука
    /// </summary>
    public bool TryRemoveEcho(Entity<AudioComponent> sound, AudioEchoEffectAffectedComponent? echoComp = null)
    {
        if (!_echoQuery.Resolve(sound, ref echoComp))
            return false;

        if (!_effectsManager.TryRemoveEffect(sound, echoComp.Preset))
            return false;

        RemComp<AudioEchoEffectAffectedComponent>(sound);
        RemComp<AudioEffectedComponent>(sound);

        return true;
    }

    private void OnEnabledToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;

        if (!enabled)
            RevertChanges();
    }

    private void OnPreferredPresetToggled(bool useStrong)
    {
        _strongPresetPreferred = useStrong;
        var newPreferredPreset = useStrong ? StrongEchoEffectPreset : StandardEchoEffectPreset;

        TogglePreset(newPreferredPreset);
    }

    /// <summary>
    /// Убирает эффекты эхо у всех звуков, что имеют его.
    /// Вызывается при выключении эффекта эха игроком.
    /// </summary>
    private void RevertChanges()
    {
        var query = AllEntityQuery<AudioEchoEffectAffectedComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var echoComp, out var audio))
        {
            TryRemoveEcho((uid, audio), echoComp);
        }
    }

    private void TogglePreset(ProtoId<AudioPresetPrototype> newPreferredPreset)
    {
        var query = AllEntityQuery<AudioEchoEffectAffectedComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var echoComp, out var audio))
        {
            if (!TryRemoveEcho((uid, audio), echoComp))
                continue;

            TryApplyEcho((uid, audio), newPreferredPreset);
        }
    }
}
