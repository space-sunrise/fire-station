namespace Content.Server._Scp.Pull;

[RegisterComponent]
public sealed partial class CanBePulledSleepingComponent : Component
{
    [DataField]
    public bool Exclusive;
}
