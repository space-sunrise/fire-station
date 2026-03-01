using Robust.Shared.Audio;

namespace Content.Server._Scp.Other.EmitSoundRandomly;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class EmitSoundRandomlyComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(20f);

    [DataField]
    public TimeSpan CooldownVariation = TimeSpan.FromSeconds(10f);

    [ViewVariables, AutoPausedField]
    public TimeSpan? NextSoundTime;
}
