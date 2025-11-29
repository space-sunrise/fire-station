using Content.Shared._Scp.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Other.EmitSoundRandomly;

public sealed class EmitSoundRandomlySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundRandomlyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EmitSoundRandomlyComponent> ent, ref ComponentStartup args)
    {
        SetNextSoundTime(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmitSoundRandomlyComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextSoundTime)
                continue;

            var ev = new BeforeRandomlyEmittingSoundEvent();
            RaiseLocalEvent(uid, ev);

            if (!ev.Cancelled)
                _audio.PlayPredicted(component.Sound, uid, uid);

            SetNextSoundTime((uid, component));
        }
    }

    private void SetNextSoundTime(Entity<EmitSoundRandomlyComponent> ent)
    {
        var variance = _random.NextFloatForEntity(ent, 0f, (float)ent.Comp.CooldownVariation.TotalSeconds);
        var cooldown = ent.Comp.SoundCooldown + TimeSpan.FromSeconds(variance);

        ent.Comp.NextSoundTime = _timing.CurTime + cooldown;
    }
}


public sealed class BeforeRandomlyEmittingSoundEvent : CancellableEntityEventArgs;
