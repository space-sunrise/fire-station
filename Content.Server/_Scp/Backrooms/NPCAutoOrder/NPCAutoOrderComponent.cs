using Robust.Shared.Serialization;

namespace Content.Server._Scp.Backrooms.NPCAutoOrder;

[RegisterComponent]
public sealed partial class NpcAutoOrderComponent : Component
{
    [DataField]
    public NpcAutoOrder Order = NpcAutoOrder.Follow;
}

[Serializable]
public enum NpcAutoOrder : byte
{
    Follow,
}
