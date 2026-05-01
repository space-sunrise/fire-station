using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Knowledge;

public enum ScpKnowledgeAcquisitionChannel : byte
{
    Listen,
    Read,
    Examine,
    Other,
}

[Flags]
[Serializable, NetSerializable]
public enum ScpKnowledgeExposureFlags : byte
{
    None = 0,
    Text = 1 << 0,
    Examine = 1 << 1,
}

public readonly record struct ScpKnowledgeSourceRecord(
    EntityUid Source,
    ProtoId<ScpKnowledgePrototype> KnowledgeId,
    ScpKnowledgeAcquisitionChannel Channel);

public readonly record struct ScpKnowledgeMessageRecord(
    EntityUid Source,
    ProtoId<ScpKnowledgePrototype> KnowledgeId,
    ScpKnowledgeAcquisitionChannel Channel,
    string NormalizedMessage);

[ByRefEvent]
public readonly record struct ScpKnowledgeUnlockedEvent(
    ProtoId<ScpKnowledgePrototype> KnowledgeId,
    ScpKnowledgeAcquisitionChannel Channel,
    EntityUid? Source = null);
