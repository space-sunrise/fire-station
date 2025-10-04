using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server._Scp.Transfers.GasTransfer;

public sealed class GasTransferSystem : EntitySystem
{
    private const float BalanceThreshold = 0.001f;
    private const float SmallTransferThreshold = 0.1f;

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTransferComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GasTransferComponent, AtmosDeviceUpdateEvent>(OnGasTransferUpdated);
    }

    private void OnMapInit(Entity<GasTransferComponent> ent, ref MapInitEvent args)
    {
        FindPartner(ent);
    }

    private void OnGasTransferUpdated(Entity<GasTransferComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.LinkId))
            return;

        if (!ent.Comp.IsActive)
            return;

        if (!ValidatePartner(ent, out var partnerPipe))
            return;

        if (ent.Owner.CompareTo(partnerPipe.Owner) > 0)
            return;

        if (!Exists(partnerPipe.Owner))
            return;

        TransferGas(ent, partnerPipe, args.dt);
    }

    private bool ValidatePartner(Entity<GasTransferComponent> ent, out PipeNode partnerPipe)
    {
        partnerPipe = default!;

        if (!ent.Comp.Partner.HasValue)
        {
            if (!FindPartner(ent))
                return false;
        }

        var partnerUid = ent.Comp.Partner!.Value;

        if (ent.Comp.PartnerTransferComp != null &&
            ent.Comp.PartnerTransferComp.Partner.HasValue &&
            ent.Comp.PartnerTransferComp.Partner.Value == ent.Owner &&
            ent.Comp.PartnerTransferComp.IsActive &&
            ent.Comp.PartnerPipe != null)
        {
            partnerPipe = ent.Comp.PartnerPipe;
            return true;
        }

        InvalidatePartner(ent.Comp);

        if (!TryComp<GasTransferComponent>(partnerUid, out var partnerTransferComp) ||
            !_nodeContainer.TryGetNode<PipeNode>(partnerUid, partnerTransferComp.InletName, out partnerPipe!))
        {
            return false;
        }

        if (!partnerTransferComp.Partner.HasValue || partnerTransferComp.Partner.Value != ent.Owner || !partnerTransferComp.IsActive)
        {
            return false;
        }

        ent.Comp.PartnerPipe = partnerPipe;
        ent.Comp.PartnerTransferComp = partnerTransferComp;

        return true;
    }

    private void InvalidatePartner(GasTransferComponent comp)
    {
        comp.Partner = null;
        comp.PartnerPipe = null;
        comp.PartnerTransferComp = null;
    }

    private bool FindPartner(Entity<GasTransferComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.LinkId))
            return false;

        var transferQuery = EntityQueryEnumerator<GasTransferComponent>();
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


    private void TransferGas(Entity<GasTransferComponent> ent, PipeNode partnerPipe, float dt)
    {
        if (!_nodeContainer.TryGetNode<PipeNode>(ent.Owner, ent.Comp.InletName, out var ourPipe))
            return;

        var maxTransfer = ent.Comp.TransferRate * dt;

        if (float.IsNaN(maxTransfer) || float.IsInfinity(maxTransfer) || maxTransfer < 0)
            return;

        switch (ent.Comp.Mode)
        {
            case GasTransferMode.Balance:
                BalanceGas(ourPipe, partnerPipe, maxTransfer);
                break;

            case GasTransferMode.Send:
                SendGas(ourPipe, partnerPipe, maxTransfer);
                break;

            case GasTransferMode.Receive:
                ReceiveGas(ourPipe, partnerPipe, maxTransfer);
                break;
        }
    }

    private void BalanceGas(PipeNode inlet, PipeNode outlet, float maxTransfer)
    {
        var totalMolesA = inlet.Air.TotalMoles;
        var totalMolesB = outlet.Air.TotalMoles;

        if (float.IsNaN(totalMolesA) || float.IsNaN(totalMolesB))
            return;

        if (float.IsNegative(totalMolesA) || float.IsNegative(totalMolesB))
            return;

        var diff = totalMolesA - totalMolesB;
        var absDiff = Math.Abs(diff);
        if (absDiff < BalanceThreshold)
            return;

        var requiredTransfer = absDiff / 2f;

        var transferAmount = Math.Min(requiredTransfer, maxTransfer);

        if (requiredTransfer < maxTransfer * SmallTransferThreshold)
            transferAmount = requiredTransfer;

        var fromNode = diff > 0 ? inlet : outlet;
        var toNode = diff > 0 ? outlet : inlet;

        TransferGasMixture(fromNode, toNode, transferAmount);
    }

    private void SendGas(PipeNode inlet, PipeNode outlet, float maxTransfer)
    {
        TransferDirectionalGas(inlet, outlet, maxTransfer);
    }

    private void ReceiveGas(PipeNode inlet, PipeNode outlet, float maxTransfer)
    {
        TransferDirectionalGas(outlet, inlet, maxTransfer);
    }

    private void TransferDirectionalGas(PipeNode source, PipeNode destination, float maxTransfer)
    {
        if (source.Air.TotalMoles <= 0)
            return;

        var transferAmount = CalculateTransferAmount(source.Air.TotalMoles, maxTransfer);
        TransferGasMixture(source, destination, transferAmount);
    }

    private float CalculateTransferAmount(float availableMoles, float maxTransfer)
    {
        if (float.IsNaN(availableMoles) || float.IsNaN(maxTransfer))
            return 0f;

        if (float.IsNegative(availableMoles) || float.IsNegative(maxTransfer))
            return 0f;

        var transferAmount = Math.Min(maxTransfer, availableMoles);

        if (transferAmount < maxTransfer * SmallTransferThreshold)
            transferAmount = availableMoles;

        return Math.Max(0f, transferAmount);
    }

    private void TransferGasMixture(PipeNode fromNode, PipeNode toNode, float moles)
    {
        if (fromNode.Air.TotalMoles <= 0)
            return;

        var removed = fromNode.Air.Remove(moles);

        _atmosphereSystem.Merge(toNode.Air, removed);
    }
}