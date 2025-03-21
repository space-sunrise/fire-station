using Content.Shared.Mobs;
using Robust.Shared.Audio;

namespace Content.Shared._Scp.Other.EmitSoundOnMobStateChanged;

[RegisterComponent]
public sealed partial class EmitSoundOnMobStateChangedComponent : Component
{
    [DataField(required: true), ViewVariables]
    public SoundSpecifier Sound;

    [DataField, ViewVariables]
    public MobState State = MobState.Dead;
}
