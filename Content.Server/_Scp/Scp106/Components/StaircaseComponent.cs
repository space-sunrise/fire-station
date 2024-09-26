namespace Content.Server._Scp.Scp106.Components;

[RegisterComponent]
public sealed partial class StaircaseComponent : Component
{
    [DataField("linkedStair")]
    public EntityUid? LinkedStair;

    public bool Generating = false;
}
