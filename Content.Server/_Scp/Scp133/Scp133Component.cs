using Content.Shared.Damage;

namespace Content.Server._Scp.Scp133;

[RegisterComponent]
public sealed partial class Scp133Component : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new() { { "Structural", 200 } }
    };

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2.5f);

    [DataField]
    public bool DeleteAfter = true;

    [ViewVariables]
    public TimeSpan? DamageTime;

    [ViewVariables]
    public EntityUid? EntTarget;
}