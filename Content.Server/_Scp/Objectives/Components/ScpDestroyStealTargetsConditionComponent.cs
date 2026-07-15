using Content.Server._Scp.Objectives.Systems;
using Content.Shared.Objectives;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent, Access(typeof(ScpDestroyStealTargetsConditionSystem))]
public sealed partial class ScpDestroyStealTargetsConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealGroup;

    [DataField]
    public int MinCollectionSize = 1;

    [DataField]
    public int MaxCollectionSize = 1;

    [ViewVariables]
    public int CollectionSize;

    [ViewVariables]
    public List<EntityUid> Targets = new();

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/bureaucracy.rsi"), "paper");
}
