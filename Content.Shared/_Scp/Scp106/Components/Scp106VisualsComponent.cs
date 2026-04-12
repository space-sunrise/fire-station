namespace Content.Shared._Scp.Scp106.Components;

[RegisterComponent]
public sealed partial class Scp106VisualsComponent : Component
{
    [DataField]
    public string BaseLayer = "base";

    [DataField(required: true)]
    public string DefaultState;

    [DataField(required: true)]
    public string EnteringState;

    [DataField(required: true)]
    public string ExitingState;
}
