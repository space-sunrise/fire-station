using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Scp.Scp106.Components;

[RegisterComponent]
public sealed partial class Scp106PortalSpawnerComponent : Component
{
    [DataField]
    public EntProtoId Monster = "MobScp106Monster";

    [DataField]
    public EntProtoId BigMonster = "MobScp106BigMonster";

    public TimeSpan NextSpawnTime;

    public float MaxSpawnedMonsters = 5f;

    public float SpawnedMonsters = 0;

    // Scp106 which spawned this portal
    public EntityUid Scp106;
}
