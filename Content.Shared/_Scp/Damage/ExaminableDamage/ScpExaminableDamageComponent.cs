using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Damage.ExaminableDamage;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpExaminableDamageComponent : Component
{
    [DataField]
    public List<string> GeneralMessages = [];

    [DataField]
    public Color Color = Color.Gray;

    [DataField]
    public ScpExaminableDamageMode Mode = ScpExaminableDamageMode.MobToDeath;
}

public enum ScpExaminableDamageMode : byte
{
    Structure,
    MobToCritical,
    MobToDeath,
}
