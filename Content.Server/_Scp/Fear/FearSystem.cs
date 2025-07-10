using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._Scp.Fear;

public sealed class FearSystem : SharedFearSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    private static readonly SoundSpecifier BreathingSound =
        new SoundPathSpecifier("/Audio/_Scp/Effects/Fear/breathing.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearActiveSoundEffectsComponent, ComponentShutdown>(OnShutdown);
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
    }
}
