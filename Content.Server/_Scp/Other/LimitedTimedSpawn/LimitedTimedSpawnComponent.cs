using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.LimitedTimedSpawn;

[RegisterComponent]
public sealed partial class LimitedTimedSpawnComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public bool CopyCopies = true;

    [DataField]
    public float Chance = 1.0f;

    [DataField]
    public int EntitiesLimit = 2;

    [DataField]
    public float ImpulseStrength = 0f;

    [DataField]
    public TimeSpan IntervalSeconds = TimeSpan.FromSeconds(60);

    [ViewVariables]
    public TimeSpan NextSpawn;
}
