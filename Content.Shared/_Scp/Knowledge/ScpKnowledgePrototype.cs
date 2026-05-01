using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Scp.Knowledge;

[Prototype]
public sealed partial class ScpKnowledgePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ScpKnowledgePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ScpKnowledgeKind Kind;

    [DataField(required: true)]
    public LocId DisplayName;

    [DataField]
    [AlwaysPushInheritance]
    public List<LocId> Abbreviations = [];

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    [AlwaysPushInheritance]
    public List<LocId> RecognitionPatterns = [];

    [DataField]
    [AlwaysPushInheritance]
    public List<EntProtoId> EntityPrototypes = [];

    [DataField]
    public int RequiredProgress = 2;

    [DataField]
    public bool AllowListen = true;

    [DataField]
    public bool AllowRead = true;

    [DataField]
    public bool AllowExamine = true;

    [DataField]
    public int ListenProgress = 1;

    [DataField]
    public int ReadProgress = 1;

    [DataField]
    public int ExamineProgress = 1;

    [DataField]
    public bool HideIdentityUntilKnown;

    [DataField]
    public LocId? KnownExamineVerbText;

    [DataField]
    public LocId? KnownExamineText;
}

public enum ScpKnowledgeKind : byte
{
    Term,
    Location,
    Entity,
}
