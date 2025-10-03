using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;

namespace Content.Server._Scp.GasTransfer;

public sealed class GasTransferSystem : EntitySystem
{
    private const float BalanceThreshold = 0.001f;
    private const float SmallTransferThreshold = 0.1f;

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTransferComponent, AtmosDeviceUpdateEvent>(OnGasTransferUpdated);
    }

    private void OnGasTransferUpdated(Entity<GasTransferComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!ent.Comp.IsActive || string.IsNullOrEmpty(ent.Comp.SourceStructureId) || string.IsNullOrEmpty(ent.Comp.TargetStructureId))
            return;

        if (!TryFindPairedDevices(ent.Comp, out var sourceUid, out var targetUid))
            return;

        if (!TryGetPipeNodes(sourceUid!.Value, targetUid!.Value, ent.Comp.InletName, out var sourcePipe, out var targetPipe))
            return;

        TransferGas(ent.Comp, sourcePipe!, targetPipe!, args.dt);
    }

    private bool TryFindPairedDevices(GasTransferComponent comp, out EntityUid? sourceUid, out EntityUid? targetUid)
    {
        sourceUid = null;
        targetUid = null;

        var transferQuery = EntityQueryEnumerator<GasTransferComponent>();
        while (transferQuery.MoveNext(out var uid, out var transferComp))
        {
            if (transferComp.SourceStructureId == comp.SourceStructureId)
                sourceUid = uid;
            if (transferComp.SourceStructureId == comp.TargetStructureId)
                targetUid = uid;

            if (sourceUid.HasValue && targetUid.HasValue)
                break;
        }

        return sourceUid.HasValue && targetUid.HasValue;
    }

    private bool TryGetPipeNodes(EntityUid sourceUid, EntityUid targetUid, string inletName, out PipeNode? sourcePipe, out PipeNode? targetPipe)
    {
        sourcePipe = null;
        targetPipe = null;

        if (!_nodeContainer.TryGetNode(sourceUid, inletName, out sourcePipe))
            return false;

        if (!_nodeContainer.TryGetNode(targetUid, inletName, out targetPipe))
            return false;

        return true;
    }

    private void TransferGas(GasTransferComponent comp, PipeNode inlet, PipeNode outlet, float dt)
    {
        var maxTransfer = comp.TransferRate * dt;

        if (float.IsNaN(maxTransfer) || float.IsInfinity(maxTransfer) || maxTransfer < 0)
            return;

        switch (comp.Mode)
        {
            case GasTransferMode.Balance:
                BalanceGas(inlet, outlet, maxTransfer);
                break;

            case GasTransferMode.Send:
                SendGas(inlet, outlet, maxTransfer);
                break;

            case GasTransferMode.Receive:
                ReceiveGas(inlet, outlet, maxTransfer);
                break;
        }
    }

    private void BalanceGas(PipeNode inlet, PipeNode outlet, float maxTransfer)
    {
        var totalMolesA = inlet.Air.TotalMoles;
        var totalMolesB = outlet.Air.TotalMoles;

        if (float.IsNaN(totalMolesA) || float.IsNaN(totalMolesB) || float.IsNegative(totalMolesA) || float.IsNegative(totalMolesB))
            return;

        var diff = totalMolesA - totalMolesB;
        if (Math.Abs(diff) < BalanceThreshold)
            return;

        var requiredTransfer = Math.Abs(diff) / 2f;
        if (float.IsNaN(requiredTransfer) || float.IsInfinity(requiredTransfer))
            return;

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
        if (float.IsNaN(availableMoles) || float.IsNaN(maxTransfer) || float.IsNegative(availableMoles) || float.IsNegative(maxTransfer))
            return 0f;

        var transferAmount = Math.Min(maxTransfer, availableMoles);

        if (transferAmount < maxTransfer * SmallTransferThreshold)
            transferAmount = availableMoles;

        return Math.Max(0f, transferAmount);
    }

    private void TransferGasMixture(PipeNode fromNode, PipeNode toNode, float moles)
    {
        if (moles <= 0 || fromNode.Air.TotalMoles <= 0 || float.IsNaN(moles) || float.IsInfinity(moles))
            return;

        var removed = fromNode.Air.Remove(moles);

        _atmosphereSystem.Merge(toNode.Air, removed);
    }
}