using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// Makes the entity immune to being flashed.
/// When given to clothes in the "head", "eyes" or "mask" slot it protects the wearer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFlashSystem))]
public sealed partial class FlashImmunityComponent : Component
{
    /// <summary>
    /// Is this component currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    // Fire added start - чтобы сцп не показывались как "устойчивые к флешкам" в описании.
    [DataField]
    public bool Examinable = true;
    // Fire added end
}
