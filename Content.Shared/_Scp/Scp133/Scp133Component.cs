using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Scp133;

[RegisterComponent, NetworkedComponent]
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