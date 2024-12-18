namespace Content.Server._Scp.Scp106.Components;

/// <summary>
/// То, откуда тепать
/// </summary>
[RegisterComponent]
public sealed partial class Scp106CatwalkCatcherComponent : Component
{
    [DataField]
    public EntityUid? StandingEnt;
}
