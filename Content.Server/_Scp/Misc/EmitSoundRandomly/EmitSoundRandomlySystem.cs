using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Misc.EmitSoundRandomly;

public sealed class EmitSoundRandomlySystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundRandomlyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EmitSoundRandomlyComponent> ent, ref MapInitEvent args)
    {
        SetNextSoundTime(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<EmitSoundRandomlyComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextSoundTime)
                continue;

            var ev = new BeforeRandomlyEmittingSoundEvent();
            RaiseLocalEvent(uid, ev);

            if (!ev.Cancelled)
                _audio.PlayPvs(component.Sound, uid);

            SetNextSoundTime((uid, component));
        }
    }

    private void SetNextSoundTime(Entity<EmitSoundRandomlyComponent> ent)
    {
        var cooldown = ent.Comp.SoundCooldown + _random.Next(ent.Comp.CooldownVariation);
        ent.Comp.NextSoundTime = _timing.CurTime + TimeSpan.FromSeconds(cooldown);
    }
}


public sealed class BeforeRandomlyEmittingSoundEvent : CancellableEntityEventArgs;
