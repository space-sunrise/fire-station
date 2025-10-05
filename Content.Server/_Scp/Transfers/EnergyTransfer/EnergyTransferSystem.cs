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
            if (string.IsNullOrEmpty(comp.LinkId))
                continue;
            if (!comp.IsActive)
                continue;

            if (!ValidatePartner((uid, comp, battery), out var partnerBattery, out var partnerUid))
                continue;

            if (!Exists(partnerUid))
                continue;

            var sourceEnt = (uid, battery, comp);
            TransferEnergy(sourceEnt, (partnerUid, partnerBattery), frameTime);
        }
    }

    private bool ValidatePartner(Entity<EnergyTransferComponent, BatteryComponent> ent, out BatteryComponent partnerBattery, out EntityUid partnerUid)
    {
        partnerBattery = default!;
        partnerUid = default;

        if (!ent.Comp1.Partner.HasValue)
        {
            if (!FindPartner(ent))
                return false;
        }

        partnerUid = ent.Comp1.Partner!.Value;

        if (ent.Comp1.PartnerEntity == null)
        {
            var partnerTransferComp = EntityManager.EnsureComponent<EnergyTransferComponent>(partnerUid);
            partnerBattery = EntityManager.EnsureComponent<BatteryComponent>(partnerUid);

            if (!partnerTransferComp.Partner.HasValue || partnerTransferComp.Partner.Value != ent.Owner || !partnerTransferComp.IsActive)
            {
                InvalidatePartner(ent.Comp1);
                return false;
            }

            ent.Comp1.PartnerEntity = (partnerUid, partnerBattery, partnerTransferComp);
        }
        else
        {
            partnerBattery = ent.Comp1.PartnerEntity.Value.Comp1;
        }

        return true;
    }

    private void InvalidatePartner(EnergyTransferComponent comp)
    {
        comp.Partner = null;
        comp.PartnerEntity = null;
    }

    private bool FindPartner(Entity<EnergyTransferComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.LinkId))
            return false;

        var transferQuery = EntityQueryEnumerator<EnergyTransferComponent>();
        while (transferQuery.MoveNext(out var otherUid, out var otherComp))
        {
            if (otherUid == ent.Owner)
                continue;

            if (string.IsNullOrEmpty(otherComp.LinkId))
                continue;

            if (otherComp.LinkId == ent.Comp.LinkId)
            {
                InvalidatePartner(ent.Comp);
                InvalidatePartner(otherComp);

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
        if (float.IsNaN(maxTransfer) || float.IsInfinity(maxTransfer))
            return;

        if (maxTransfer < 0)
            return;
            
        switch (sourceEnt.Comp2.Mode)
        {
            case EnergyTransferMode.Balance:
                BalanceEnergy(sourceEnt, targetEnt, maxTransfer);
                break;

            case EnergyTransferMode.Send:
                SendEnergy(sourceEnt, targetEnt, maxTransfer);
                break;

            case EnergyTransferMode.Receive:
                ReceiveEnergy(sourceEnt, targetEnt, maxTransfer);
                break;
        }
    }

    private void BalanceEnergy(Entity<BatteryComponent> sourceEnt, Entity<BatteryComponent> targetEnt, float maxTransfer)
    {
        var chargeDiff = sourceEnt.Comp.CurrentCharge - targetEnt.Comp.CurrentCharge;
        var absChargeDiff = Math.Abs(chargeDiff);
        if (absChargeDiff < BalanceThreshold)
            return;

        var transferAmount = Math.Min(absChargeDiff / 2f, maxTransfer);

        Entity<BatteryComponent> fromEntity, toEntity;
        if (chargeDiff > 0)
        {
            fromEntity = sourceEnt;
            toEntity = targetEnt;
        }
        else
        {
            fromEntity = targetEnt;
            toEntity = sourceEnt;
        }

        TransferEnergyBetween(fromEntity, toEntity, transferAmount);
    }

    private void TransferEnergyBetween(Entity<BatteryComponent> fromEntity, Entity<BatteryComponent> toEntity, float transferAmount)
    {
        var availableCapacity = toEntity.Comp.MaxCharge - toEntity.Comp.CurrentCharge;
        transferAmount = Math.Min(transferAmount, availableCapacity);

        if (transferAmount > 0)
        {
            TransferCharge(fromEntity, -transferAmount);
            TransferCharge(toEntity, transferAmount);
        }
    }

    private void SendEnergy(Entity<BatteryComponent> sourceEnt, Entity<BatteryComponent> targetEnt, float maxTransfer)
    {
        if (sourceEnt.Comp.CurrentCharge <= 0)
            return;

        var availableCapacity = targetEnt.Comp.MaxCharge - targetEnt.Comp.CurrentCharge;
        if (availableCapacity <= 0)
            return;

        var transferAmount = Math.Min(Math.Min(maxTransfer, sourceEnt.Comp.CurrentCharge), availableCapacity);
        if (transferAmount > 0)
        {
            TransferCharge(sourceEnt, -transferAmount);
            TransferCharge(targetEnt, transferAmount);
        }
    }

    private void ReceiveEnergy(Entity<BatteryComponent> sourceEnt, Entity<BatteryComponent> targetEnt, float maxTransfer)
    {
        if (targetEnt.Comp.CurrentCharge <= 0)
            return;

        var availableCapacity = sourceEnt.Comp.MaxCharge - sourceEnt.Comp.CurrentCharge;
        if (availableCapacity <= 0)
            return;

        var transferAmount = Math.Min(Math.Min(maxTransfer, targetEnt.Comp.CurrentCharge), availableCapacity);
        if (transferAmount > 0)
        {
            TransferCharge(targetEnt, -transferAmount);
            TransferCharge(sourceEnt, transferAmount);
        }
    }

    private void TransferCharge(Entity<BatteryComponent> entity, float amount)
    {
        _battery.SetCharge(entity.Owner, entity.Comp.CurrentCharge + amount, entity.Comp);
    }
}