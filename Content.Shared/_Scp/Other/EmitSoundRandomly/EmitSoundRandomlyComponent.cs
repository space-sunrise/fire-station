using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.EmitSoundRandomly;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EmitSoundRandomlyComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(20f);

    [DataField]
    public TimeSpan CooldownVariation = TimeSpan.FromSeconds(10f);

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextSoundTime;
}
