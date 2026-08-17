
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ScpOnSoundVisibilityViewerComponent : Component
{
    [DataField]
    public bool PoorEyesOnFlash;

    [DataField, AutoNetworkedField]
    public bool PoorEyesight;

    [DataField]
    public TimeSpan PoorEyesightTime = TimeSpan.FromSeconds(10f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan? PoorEyesightTimeStart; // Когда начали плохо видеть

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float VisibilityActivationRange = 20f;

    [DataField]
    public LocId? OnFlashMessage;

    [DataField]
    public EntityWhitelist Protections = new()
    {
        Components = ["ScpOnSoundVisibilityProtection"]
    };

    [DataField]
    public float ExamineHideThreshold = 0.2f;

    [DataField]
    public float StatusIconClearThreshold = 0.5f;
}
