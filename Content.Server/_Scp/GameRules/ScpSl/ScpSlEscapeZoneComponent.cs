using Content.Shared._Scp.GameRule.Sl;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameRules.ScpSl;

[RegisterComponent]
public sealed partial class ScpSlEscapeZoneComponent : Component;

[RegisterComponent]
public sealed partial class SlHumanoidSpawnPointComponent : Component
{
    [DataField(required: true)]
    public ScpSlHumanoidType SpawnPointType { get; private set; }
}

[RegisterComponent]
public sealed partial class SlScpSpawnPointComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ScpProtoId { get; private set; }

    [DataField]
    public bool Playable { get; private set; } = true;
}

