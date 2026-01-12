using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp096.Photo;

/// <summary>
/// Компонент-маркер фотографии SCP-096.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class Scp096PhotoComponent : Component;

[Serializable, NetSerializable]
public enum Scp096PhotoVisualLayers : byte
{
    Base,
}
