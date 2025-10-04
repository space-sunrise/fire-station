using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Robust.Shared.Map;

namespace Content.Server._Scp.Transfers.EnergyTransfer;

public sealed class EnergyTransferSystem : EntitySystem
{
    private const float BalanceThreshold = 0.1f;

    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyTransferComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EnergyTransferComponent> ent, ref MapInitEvent args)
    {
        FindPartner(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EnergyTransferComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var comp, out var battery))
        {
            if (!comp.IsActive)
                continue;

            if (string.IsNullOrEmpty(comp.LinkId))
                continue;

            if (!ValidatePartner((uid, comp), out var partnerBattery, out var partnerUid))
                continue;

            if (uid > partnerUid)
                continue;

            var sourceEnt = (uid, battery, comp);
            TransferEnergy(sourceEnt, (partnerUid, partnerBattery), frameTime);
        }
    }

    private bool ValidatePartner(Entity<EnergyTransferComponent> ent, out BatteryComponent partnerBattery, out EntityUid partnerUid)
    {
        partnerBattery = default!;
        partnerUid = default;

        if (!ent.Comp.Partner.HasValue)
        {
            if (!FindPartner(ent))
                return false;
        }

        partnerUid = ent.Comp.Partner!.Value;

        if (!Exists(partnerUid))
        {
            ent.Comp.Partner = null;
            return false;
        }

        if (!TryComp<BatteryComponent>(partnerUid, out partnerBattery!) ||
            !TryComp<EnergyTransferComponent>(partnerUid, out var transferComp))
        {
            ent.Comp.Partner = null;
            return false;
        }

        if (!transferComp.Partner.HasValue || transferComp.Partner.Value != ent.Owner)
        {
            if (!FindPartner(ent))
                return false;
        }

        return true;
    }

    private bool FindPartner(Entity<EnergyTransferComponent> ent)
    {
        var transferQuery = EntityQueryEnumerator<EnergyTransferComponent>();
        while (transferQuery.MoveNext(out var otherUid, out var otherComp))
        {
            if (otherUid == ent.Owner)
                continue;

            if (otherComp.LinkId == ent.Comp.LinkId)
            {
                ent.Comp.Partner = otherUid;
                otherComp.Partner = ent.Owner;
                return true;
            }
        }

        return false;
    }

    private void TransferEnergy(Entity<BatteryComponent, EnergyTransferComponent> sourceEnt, Entity<BatteryComponent> targetEnt, float frameTime)
    {
        var maxTransfer = sourceEnt.Comp2.TransferRate * frameTime;
        if (float.IsNaN(maxTransfer) || float.IsInfinity(maxTransfer) || maxTransfer <= 0f)
            return;

        switch (sourceEnt.Comp2.Mode)
        {
            case EnergyTransferMode.Balance:
                BalanceEnergy(sourceEnt.Comp1, targetEnt.Comp, sourceEnt.Owner, targetEnt.Owner, maxTransfer);
                break;

            case EnergyTransferMode.Send:
                SendEnergy(sourceEnt.Comp1, targetEnt.Comp, sourceEnt.Owner, targetEnt.Owner, maxTransfer);
                break;

            case EnergyTransferMode.Receive:
                ReceiveEnergy(sourceEnt.Comp1, targetEnt.Comp, sourceEnt.Owner, targetEnt.Owner, maxTransfer);
                break;
        }
    }

    private void BalanceEnergy(BatteryComponent batteryA, BatteryComponent batteryB, EntityUid sourceUid, EntityUid targetUid, float maxTransfer)
    {
        var chargeDiff = batteryA.CurrentCharge - batteryB.CurrentCharge;
        var absChargeDiff = Math.Abs(chargeDiff);
        if (absChargeDiff < BalanceThreshold)
            return;

        var transferAmount = Math.Min(absChargeDiff / 2f, maxTransfer);

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