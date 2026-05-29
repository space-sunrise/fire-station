using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.LimitedTimedSpawn;

[RegisterComponent]
public sealed partial class LimitedTimedSpawnComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float Chance = 1.0f;

    [DataField]
    public int EntitiesLimit = 2;

    [DataField]
    public float ImpulseStrength = 0f;

    [DataField]
    public TimeSpan IntervalSeconds = TimeSpan.FromSeconds(60);

    [ViewVariables]
    public int? EntityIdentificator;

    [ViewVariables]
    public TimeSpan NextSpawn;
}
