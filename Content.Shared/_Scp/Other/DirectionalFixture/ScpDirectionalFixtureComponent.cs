using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.DirectionalFixture;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ScpDirectionalFixtureSystem))]
public sealed partial class ScpDirectionalFixtureComponent : Component
{
    [DataField]
    public string FixtureId = "fix1";

    [DataField]
    public Dictionary<Direction, Box2> BoundingBoxes = new();

    [AutoNetworkedField, ViewVariables]
    public Box2? DefaultBoundingBox;
}
