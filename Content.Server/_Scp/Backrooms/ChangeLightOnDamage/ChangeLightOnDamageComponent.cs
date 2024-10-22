using Content.Shared.FixedPoint;

namespace Content.Server._Scp.Backrooms.ChangeLightOnDamage;

[RegisterComponent]
public sealed partial class ChangeLightOnDamageComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public Color TargetLightColor;

    // TODO: Получать значение из DestructibleComponent, а не так. Но щас впадлу разбираться
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxDamage;

}
