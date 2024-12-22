using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp939;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp939VisibilityComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float VisibilityAcc;

    public readonly float HideTime = 2.5f;

    #region PoorEyesight

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinValue = 40;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxValue = 400;

    #endregion


}
