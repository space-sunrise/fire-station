using Content.Shared._Scp.Knowledge;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Knowledge.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpKnowledgeComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ScpKnowledgePrototype>> KnownKnowledge = [];

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ScpKnowledgePrototype>, int> Progress = [];

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ScpKnowledgePrototype>, ScpKnowledgeExposureFlags> ExposureFlags = [];

    [NonSerialized]
    public HashSet<ScpKnowledgeSourceRecord> ProcessedSources = [];

    [NonSerialized]
    public HashSet<ScpKnowledgeMessageRecord> ProcessedMessages = [];
}
