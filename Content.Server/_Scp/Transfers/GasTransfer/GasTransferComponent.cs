namespace Content.Server._Scp.Transfers.GasTransfer;

[RegisterComponent]
public sealed partial class GasTransferComponent : Component
{
    [DataField]
    public string LinkId = "";

    [DataField]
    public string InletName = "inlet";

    [DataField]
    public float TransferRate = 100f;

    [DataField]
    public bool IsActive = true;

    [DataField]
    public GasTransferMode Mode = GasTransferMode.Balance;

    [ViewVariables]
    public EntityUid? Partner;

    [ViewVariables]
    public Content.Server.NodeContainer.Nodes.PipeNode? PartnerPipe;

    [ViewVariables]
    public GasTransferComponent? PartnerTransferComp;
}

public enum GasTransferMode
{
    Balance,
    Send,
    Receive,
}