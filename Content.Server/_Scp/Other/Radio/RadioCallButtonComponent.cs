using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.Radio;

/// <summary>
/// Contains configuration and cooldown state for a physical button that broadcasts localized messages to a radio channel. Consumed by RadioCallButtonSystem.
/// </summary>

[RegisterComponent]
public sealed partial class RadioCallButtonComponent : Component
{
    /// <summary>
    /// The message key to send to the radio.
    /// </summary>
    [DataField(required: true)]
    public string MessageKey = string.Empty;

    /// <summary>
    /// The channel to send the message to.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<RadioChannelPrototype>> RadioChannel = default!;

    /// <summary>
    /// The room name of where the call is coming from.
    /// </summary>
    [DataField]
    public string? RoomName;

    /// <summary>
    /// Maximum search radius for beacons.
    /// </summary>
    [DataField]
    public float BeaconSearchRadius = 15f;
}
