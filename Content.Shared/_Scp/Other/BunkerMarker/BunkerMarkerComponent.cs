using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.BunkerMarker;

[RegisterComponent, NetworkedComponent]
public sealed partial class BunkerMarkerComponent : Component
{
    [DataField]
    public float Radius = 1.5f;

    [DataField]
    public string BunkerBlockFixtureId  = "ScpBunkerBlock";

    [DataField]
    public string BunkerSensorFixtureId = "ScpBunkerSensor";

    [DataField]
    public float  BunkerBlockFixtureHalfExtent = 0.5f;

    [DataField]
    public int BunkerBlockCollision = (int)(CollisionGroup.MobMask | CollisionGroup.GhostImpassable);

    [DataField]
    public int BunkerSensorCollisionMask = (int)(CollisionGroup.MobLayer | CollisionGroup.GhostImpassable);

    [DataField]
    public int BunkerSensorCollisionLayer = (int)CollisionGroup.GhostImpassable;
}
