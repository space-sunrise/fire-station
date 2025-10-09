using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;

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
        TryFindPartner(ent.AsNullable());
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

            if (!ValidatePartner((uid, comp, battery), out var partner))
                continue;

            var sourceEnt = (uid, battery, comp);
            TransferEnergy(sourceEnt, (partner.Value, partner.Value.Comp2), frameTime);
        }
    }

    private bool ValidatePartner(Entity<EnergyTransferComponent, BatteryComponent> ent, [NotNullWhen(true)] out Entity<EnergyTransferComponent, BatteryComponent>? partner)
    {
        partner = null;

        if (!Exists(ent.Comp1.Partner) || !ent.Comp1.Partner.HasValue)
        {
            if (!TryFindPartner(ent.AsNullable()))
                return false;
        }

        if (ent.Comp1.Partner!.Value.Comp1.Deleted || ent.Comp1.Partner!.Value.Comp2.Deleted)
        {
            if (!TryFindPartner(ent.AsNullable()))
                return false;
        }

        partner = ent.Comp1.Partner;

        return true;
    }

    private bool TryFindPartner(Entity<EnergyTransferComponent?, BatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        if (string.IsNullOrEmpty(ent.Comp1.LinkId))
            return false;

        var transferQuery = EntityQueryEnumerator<EnergyTransferComponent, BatteryComponent>();
        while (transferQuery.MoveNext(out var otherUid, out var otherComp, out var batteryOther))
        {
            if (otherUid == ent.Owner)
                continue;

            if (string.IsNullOrEmpty(otherComp.LinkId))
                continue;

            if (otherComp.LinkId != ent.Comp1.LinkId)
                continue;

            ent.Comp1.Partner = (otherUid, otherComp, batteryOther);
            otherComp.Partner = (ent, ent.Comp1, ent.Comp2);

            return true;
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

        if (chargeDiff > 0)
            TransferEnergyBetween(sourceEnt, targetEnt, transferAmount);
        else
            TransferEnergyBetween(targetEnt, sourceEnt, transferAmount);
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
