using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Audio.Systems;

public sealed class EchoEffectSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<AudioPresetPrototype> EchoEffectPreset = "Bathroom";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AudioComponent> ent, ref ComponentInit args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Exists(ent))
            return;

        // Фоновая музыка не должна подвергаться эффектам эха
        if (ent.Comp.Global)
            return;

        _effectsManager.TryAddEffect(ent, EchoEffectPreset);
    }
}
