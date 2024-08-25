namespace Content.Shared._Scp.Scp999;

[RegisterComponent]
public sealed partial class Scp999Component : Component
{
    [DataField]
    public Scp999States CurrentState = Scp999States.Default;

    [DataField]
    public Dictionary<Scp999States, string> States = new();
}
