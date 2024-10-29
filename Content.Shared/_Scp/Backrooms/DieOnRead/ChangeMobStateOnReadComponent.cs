using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Backrooms.DieOnRead;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangeMobStateOnReadComponent : Component
{
    [DataField, ViewVariables]
    public MobState State;
}
