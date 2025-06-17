using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._Scp.Other.AirlockManEater;

[RegisterComponent]
public sealed partial class AirlockManEaterComponent : Component
{
    [ViewVariables]
    public const float AutoCloseModifier = 100f;

    [ViewVariables]
    public const float TimeModifier = 2f;

    [ViewVariables]
    public readonly TimeSpan StunTime = TimeSpan.FromSeconds(5f);

    [ViewVariables]
    public readonly DamageSpecifier CrushDamage = new DamageSpecifier
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 30f },
        },
    };
}
