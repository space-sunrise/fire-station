using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum DiskConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DiskConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool CanPrint;
    public Dictionary<ProtoId<ResearchPointPrototype>, int> PointCost;
    public Dictionary<ProtoId<ResearchPointPrototype>, int> ServerPoints;

    public DiskConsoleBoundUserInterfaceState(Dictionary<ProtoId<ResearchPointPrototype>, int> serverPoints, Dictionary<ProtoId<ResearchPointPrototype>, int> pointCost, bool canPrint)
    {
        CanPrint = canPrint;
        PointCost = pointCost;
        ServerPoints = serverPoints;
    }
}

[Serializable, NetSerializable]
public sealed class DiskConsolePrintDiskMessage : BoundUserInterfaceMessage
{

}
