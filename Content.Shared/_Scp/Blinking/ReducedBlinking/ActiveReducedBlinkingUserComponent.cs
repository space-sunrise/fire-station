using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Blinking.ReducedBlinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveReducedBlinkingUserComponent : Component
{
    [DataField(required:true), AutoNetworkedField]
    public TimeSpan BlinkingBonusTime;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Duration;
}
