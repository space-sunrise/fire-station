
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpOnSoundVisibilityViewerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool PoorEyesOnFlash = false;

    [DataField, AutoNetworkedField]
    public bool PoorEyesight;

    [DataField, AutoNetworkedField]
    public float PoorEyesightTime = 10f; // Секунды

    [AutoNetworkedField]
    public TimeSpan? PoorEyesightTimeStart; // Когда начали плохо видеть

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float VisibilityActivationRange = 20f;

    [DataField, AutoNetworkedField]
    public LocId? OnFlashMessage;

    [DataField, AutoNetworkedField]
    public EntityWhitelist Protections = new()
    {
        Components = new[]
        {
            "ScpOnSoundVisibilityProtection"
        }
    };
}
