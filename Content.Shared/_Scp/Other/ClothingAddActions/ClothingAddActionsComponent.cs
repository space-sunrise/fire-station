using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ClothingAddActions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ClothingAddActionsSystem))]
public sealed partial class ClothingAddActionsComponent : Component
{
    /// <summary>
    /// Список акшенов, которые будут добавлены таргету при надевании и убраны при снятии одежды
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<EntProtoId> Actions = new();

    /// <summary>
    /// Вспомогательный лист с EntityUid акшенов, чтобы забирать их у персонажа
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();

    [AutoNetworkedField]
    public EntityUid? ActionsOwner;
}
