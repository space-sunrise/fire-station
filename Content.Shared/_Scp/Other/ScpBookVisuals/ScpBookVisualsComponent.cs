using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.ScpBookVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpBookVisualsComponent : Component
{

}

[Serializable, NetSerializable]
public enum ScpBookVisuals
{
    Base,
}

[Serializable, NetSerializable]
public enum ScpBookVisualLayers
{
    ScpBookVisualState,
}

[Serializable, NetSerializable]
public enum ScpBookVisualState
{
    Closed,
    Open,
    OpenWritten,
}
