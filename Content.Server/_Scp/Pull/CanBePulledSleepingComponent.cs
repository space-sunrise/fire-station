namespace Content.Server._Scp.Pull;

[RegisterComponent]
public sealed partial class CanBePulledSleepingComponent : Component
{
    /// <summary>
    /// Нужно ли удалять старый <see cref="PullableComponent"/>
    /// </summary>
    [DataField]
    public bool Exclusive;
}
