using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.Blackout.PowerLever;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class PowerLeverComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public bool Toggled;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blackout/lever_toggled.ogg");
}

[Serializable, NetSerializable]
public enum PowerLevelVisualLayers : byte
{
    Base,
    Toggled,
}
