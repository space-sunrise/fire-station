using Content.Shared.Research;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    [DataField("pointspersecond"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ProtoId<ResearchPointPrototype>, int> PointsPerSecond = [];

    [DataField("active"), ViewVariables(VVAccess.ReadWrite)]
    public bool Active;
}
