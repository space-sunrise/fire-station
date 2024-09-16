using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameRules.ScpSl;

[RegisterComponent]
public sealed partial class ScpSlSpawnComponent : Component
{
    public EntProtoId<ScpMarkerComponent>? ScpProto { get; set; }

    [DataField(required: true)]
    public ScpSlSpawnType SpawnType;
}

public enum ScpSlSpawnType
{
    ClassD,
    Scientist,
    Security,
    Mog,
    Chaos,
    Scp
}
