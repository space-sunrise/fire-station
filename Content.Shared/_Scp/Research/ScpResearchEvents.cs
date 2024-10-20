using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Research;

#region Base

[Serializable, NetSerializable]
public abstract partial class BaseScpInteractDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    public Entity<ScpResearchToolComponent> Tool;

    public BaseScpInteractDoAfterEvent() {}

    public BaseScpInteractDoAfterEvent(Entity<ScpResearchToolComponent> tool)
    {
        Tool = tool;
    }
}

[Serializable, NetSerializable, DataDefinition]
public abstract partial class BaseScpSpawnInteractDoAfterEvent : BaseScpInteractDoAfterEvent
{
    [DataField]
    public EntProtoId ToSpawn;

    public BaseScpSpawnInteractDoAfterEvent() {}

    public BaseScpSpawnInteractDoAfterEvent(EntProtoId toSpawn)
    {
        ToSpawn = toSpawn;
    }
}

#endregion


#region Additional events

[Serializable, NetSerializable, DataDefinition]
public sealed partial class Scp173PickaxeInteractDoAfterEvent : BaseScpSpawnInteractDoAfterEvent {}


[Serializable, NetSerializable, DataDefinition]
public sealed partial class Scp173CrowbarInteractDoAfterEvent : BaseScpSpawnInteractDoAfterEvent {}

#endregion

