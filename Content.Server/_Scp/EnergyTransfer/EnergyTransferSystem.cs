using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;

namespace Content.Server._Scp.EnergyTransfer;

public sealed class EnergyTransferSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EnergyTransferComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsActive || string.IsNullOrEmpty(comp.SourceStructureId) || string.IsNullOrEmpty(comp.TargetStructureId))
                continue;

            EntityUid? sourceUid = null;
            EntityUid? targetUid = null;

            var transferQuery = EntityQueryEnumerator<EnergyTransferComponent>();
            while (transferQuery.MoveNext(out var entUid, out var transferComp))
            {
                if (transferComp.SourceStructureId == comp.SourceStructureId)
                    sourceUid = entUid;
                if (transferComp.SourceStructureId == comp.TargetStructureId)
                    targetUid = entUid;

                if (sourceUid.HasValue && targetUid.HasValue)
                    break;
            }

            if (!sourceUid.HasValue || !targetUid.HasValue)
                continue;

            if (!TryComp<BatteryComponent>(sourceUid.Value, out var batteryA) ||
                !TryComp<BatteryComponent>(targetUid.Value, out var batteryB))
                continue;

            TransferEnergy(comp, batteryA, batteryB, sourceUid.Value, targetUid.Value, frameTime);
        }
    }

    private void TransferEnergy(EnergyTransferComponent comp, BatteryComponent batteryA, BatteryComponent batteryB, EntityUid sourceUid, EntityUid targetUid, float frameTime)
    {
        var maxTransfer = comp.TransferRate * frameTime;

        switch (comp.Mode)
        {
            case EnergyTransferMode.Balance:
                BalanceEnergy(batteryA, batteryB, sourceUid, targetUid, maxTransfer);
                break;

            case EnergyTransferMode.Send:
                SendEnergy(batteryA, batteryB, sourceUid, targetUid, maxTransfer);
                break;

            case EnergyTransferMode.Receive:
                ReceiveEnergy(batteryA, batteryB, sourceUid, targetUid, maxTransfer);
                break;
        }
    }

    private void BalanceEnergy(BatteryComponent batteryA, BatteryComponent batteryB, EntityUid sourceUid, EntityUid targetUid, float maxTransfer)
    {
        var chargeDiff = batteryA.CurrentCharge - batteryB.CurrentCharge;
        if (Math.Abs(chargeDiff) < 0.1f)
            return;

        var transferAmount = Math.Min(Math.Abs(chargeDiff) / 2f, maxTransfer);

        if (chargeDiff > 0)
        {
            TransferCharge(sourceUid, batteryA, -transferAmount);
            TransferCharge(targetUid, batteryB, transferAmount);
        }
        else
        {
            TransferCharge(targetUid, batteryB, -transferAmount);
            TransferCharge(sourceUid, batteryA, transferAmount);
        }
    }

    private void SendEnergy(BatteryComponent batteryA, BatteryComponent batteryB, EntityUid sourceUid, EntityUid targetUid, float maxTransfer)
    {
        if (batteryA.CurrentCharge <= 0)
            return;

        var transferAmount = Math.Min(maxTransfer, batteryA.CurrentCharge);
        TransferCharge(sourceUid, batteryA, -transferAmount);
        TransferCharge(targetUid, batteryB, transferAmount);
    }

    private void ReceiveEnergy(BatteryComponent batteryA, BatteryComponent batteryB, EntityUid sourceUid, EntityUid targetUid, float maxTransfer)
    {
        if (batteryB.CurrentCharge <= 0)
            return;

        var transferAmount = Math.Min(maxTransfer, batteryB.CurrentCharge);
        TransferCharge(targetUid, batteryB, -transferAmount);
        TransferCharge(sourceUid, batteryA, transferAmount);
    }

    private void TransferCharge(EntityUid uid, BatteryComponent battery, float amount)
    {
        _battery.SetCharge(uid, battery.CurrentCharge + amount, battery);
    }
}