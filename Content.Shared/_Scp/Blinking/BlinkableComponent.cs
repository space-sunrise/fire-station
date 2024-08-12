using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Blinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlinkableComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BlinkingAlert = "Blinking";

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextBlink;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan BlinkEndTime;
}
