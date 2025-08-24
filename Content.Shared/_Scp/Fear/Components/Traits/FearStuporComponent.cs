using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Components.Traits;

/// <summary>
/// Компонент, отвечающий за возможность попасть в состояние оцепенения.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FearStuporComponent : Component
{
    [DataField, ViewVariables]
    public FearState RequiredState = FearState.Fear;

    [DataField, ViewVariables]
    public float Chance = 10f;

    [DataField, ViewVariables]
    public TimeSpan StuporTime = TimeSpan.FromSeconds(10f);

    public static readonly EntProtoId StatusEffect = "StatusEffectFearStupor";
}
