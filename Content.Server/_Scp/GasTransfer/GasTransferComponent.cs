namespace Content.Server._Scp.GasTransfer;

[RegisterComponent]
public sealed partial class GasTransferComponent : Component
{
    [DataField]
    public string SourceStructureId = "";

    [DataField]
    public string TargetStructureId = "";

    [DataField]
    public string InletName = "inlet";

    [DataField]
    public float TransferRate = 100f;

    [DataField]
    public bool IsActive = true;

    [DataField]
    public GasTransferMode Mode = GasTransferMode.Balance;
}

public enum GasTransferMode
{
    Balance,
    Send,
    Receive,
}