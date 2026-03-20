using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.BunkerMarker;

[RegisterComponent, NetworkedComponent]
public sealed partial class BunkerMarkerComponent : Component
{
    [DataField]
    public float Radius = 1.5f;

    public const string BunkerBlockFixtureId  = "ScpBunkerBlock";
    public const string BunkerSensorFixtureId = "ScpBunkerSensor";
    public const float  BunkerBlockFixtureRadius = 0.4f;
}
