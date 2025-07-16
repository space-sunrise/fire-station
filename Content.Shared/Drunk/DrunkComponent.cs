using Robust.Shared.GameStates;

namespace Content.Shared.Drunk;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DrunkComponent : Component
{
    // Fire edit start - для уменьшения уровня страха от алкоголя
    [ViewVariables, AutoNetworkedField]
    public float CurrentBoozePower;
    // Fire edit end
}
