using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ShowMessageOnSpawn;

/// <summary>
/// Высвечивает на экране окошко с сообщением, один раз.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowMessageOnSpawnComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Message = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public bool MessageShowed;
}
