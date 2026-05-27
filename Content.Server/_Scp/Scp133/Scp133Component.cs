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
    public float Delay = 2.5f;

    [DataField]
    public bool DeleteAfter = true;
}