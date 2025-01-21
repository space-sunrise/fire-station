using Robust.Shared.Audio;

namespace Content.Server._Scp.Misc.EmitSoundRandomly;

[RegisterComponent]
public sealed partial class EmitSoundRandomlyComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SoundCooldown = 20f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CooldownVariation = 10f;

    public TimeSpan? NextSoundTime;
}
