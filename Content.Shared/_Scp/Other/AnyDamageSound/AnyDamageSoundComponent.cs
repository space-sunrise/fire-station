using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.AnyDamageSound;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnyDamageSoundComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public bool RequireExistingOrigin = true;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan? NextSoundTime;
}
