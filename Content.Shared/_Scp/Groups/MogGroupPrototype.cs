using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Scp.Groups;

[Serializable, Prototype("groupSpawn")]
public sealed partial class MogGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; set; } = string.Empty;

    [DataField("minDistance")]
    public float MinDistance { get; set; }

    [DataField("maxDistance")]
    public float MaxDistance { get; set; }

    [ViewVariables(VVAccess.ReadOnly),
     DataField("group")]
    public List<MogGroupEntry> Group = [];
}

[DataDefinition]
public sealed partial class MogGroupEntry
{
    [DataField("entity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity;

    [DataField("amount")]
    public int Amount;
}
