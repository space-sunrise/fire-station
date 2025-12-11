using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp096.Photo;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp096PhotoComponent : Component
{

}

[Serializable, NetSerializable]
public enum Scp096PhotoVisualLayers : byte
{
    Base,
}
