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
public sealed partial class ScpSpawnInteractDoAfterEvent : BaseScpInteractDoAfterEvent
{
    [DataField]
    public EntProtoId ToSpawn;

    public ScpSpawnInteractDoAfterEvent() {}

    public ScpSpawnInteractDoAfterEvent(EntProtoId toSpawn)
    {
        ToSpawn = toSpawn;
    }
}

#endregion

