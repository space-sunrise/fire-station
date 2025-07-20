using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear.Components.Traits;

[RegisterComponent, NetworkedComponent]
public sealed partial class FearStutteringComponent : Component
{
    [DataField, ViewVariables]
    public FearState RequiredState = FearState.Anxiety;
}
