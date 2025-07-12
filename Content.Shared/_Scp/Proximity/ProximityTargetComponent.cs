using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Proximity;

/// <summary>
/// Компонент-маркер цели, которая будет вызывать ивент при приближении к <see cref="ProximityReceiverComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProximityTargetComponent : Component
{

}
