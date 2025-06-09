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

    private static readonly ProtoId<AudioPresetPrototype> EchoEffectPreset = "SewerPipe";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentInit>(OnInit, before: [typeof(SharedAudioSystem)]);
    }

    private void OnInit(Entity<AudioComponent> ent, ref ComponentInit args)
    {
        ApplyEcho(ent);
    }

    public void ApplyEcho(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype>? preset = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(sound) || Paused(sound))
            return;

        // Фоновая музыка не должна подвергаться эффектам эха
        if (sound.Comp.Global)
            return;

        _effectsManager.TryAddEffect(sound, preset ?? EchoEffectPreset);
    }
}
