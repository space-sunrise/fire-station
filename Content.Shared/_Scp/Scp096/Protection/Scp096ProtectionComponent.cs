using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Protection;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp096ProtectionComponent : Component
{
    [DataField]
    public float ProblemChance
    {
        get => _problecChance;
        set => _problecChance = Math.Clamp(value, 0f, 1f);
    }

    private float _problecChance;
}
