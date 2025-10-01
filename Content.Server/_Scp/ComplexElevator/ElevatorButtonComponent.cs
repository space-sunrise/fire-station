namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ElevatorButtonComponent : Component
{
    [DataField]
    public string ElevatorId = string.Empty;

    [DataField]
    public ElevatorButtonType ButtonType = ElevatorButtonType.CallButton;

    [DataField]
    public string Floor = string.Empty;

    [DataField]
    public TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    public TimeSpan? LastUsed;
}

public enum ElevatorButtonType
{
    CallButton,
    SendElevatorDown,
    SendElevatorUp,
}
