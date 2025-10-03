namespace Content.Server._Scp.EnergyTransfer;

[RegisterComponent]
public sealed partial class EnergyTransferComponent : Component
{
    [DataField]
    public string SourceStructureId = "";

    [DataField]
    public string TargetStructureId = "";

    [DataField]
    public float TransferRate = 150000f;

    [DataField]
    public bool IsActive = true;

    [DataField]
    public EnergyTransferMode Mode = EnergyTransferMode.Balance;
}

public enum EnergyTransferMode
{
    Balance,
    Send,
    Receive,
}