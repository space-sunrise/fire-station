using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.MetaGarbageSpawn;

[RegisterComponent]
public sealed partial class MetaGarbageSpawnRuleComponent : Component
{
    [DataField]
    public EntProtoId SuccessfullySpawnedDocumentRule = "SendDocumentMetaGarbageSuccessful";

    [DataField]
    public EntProtoId FailSpawnedDocumentRule = "SendDocumentMetaGarbageFailed";
}
