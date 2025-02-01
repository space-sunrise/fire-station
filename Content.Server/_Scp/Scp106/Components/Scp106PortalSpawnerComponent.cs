using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Scp.Scp106.Components;

[RegisterComponent]
public sealed partial class Scp106PortalSpawnerComponent : Component
{
    [DataField("monster", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Monster = "MobScp106Monster";

    [DataField("bigMonster", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BigMonster = "MobScp106BigMonster";

    public float Accumulator;

    public float MaxSpawnedMonsters = 5f;

    public float SpawnedMonsters = 0;

    // Scp106 which spawned this portal
    public EntityUid Scp106;
}
