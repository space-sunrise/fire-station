using Content.Shared.Mobs;
using Robust.Server.Audio;

namespace Content.Server._Scp.Misc.EmitSoundOnMobStateChanged;

public sealed class EmitSoundOnMobStateChangedSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnMobStateChangedComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<EmitSoundOnMobStateChangedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != ent.Comp.State)
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent);
    }
}
