using Content.Server.Power.Components;

namespace Content.Server._Scp.Transfers.EnergyTransfer;

[RegisterComponent]
public sealed partial class EnergyTransferComponent : Component
{
    [DataField]
    public string LinkId = "";

    [DataField]
    public float TransferRate = 150000f;

    [DataField]
    public bool IsActive = true;

    [DataField]
    public EnergyTransferMode Mode = EnergyTransferMode.Balance;

    [ViewVariables]
    public EntityUid? Partner;

    [ViewVariables]
    public BatteryComponent? PartnerBattery;

    [ViewVariables]
    public EnergyTransferComponent? PartnerTransferComp;
}

public enum EnergyTransferMode
{
    Balance,
    Send,
    Receive,
}