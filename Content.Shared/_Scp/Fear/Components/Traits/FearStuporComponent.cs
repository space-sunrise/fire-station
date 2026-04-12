using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Components.Traits;

/// <summary>
/// Компонент, отвечающий за возможность попасть в состояние оцепенения.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FearStuporComponent : Component
{
    [DataField]
    public FearState RequiredState = FearState.Fear;

    [DataField]
    public float Chance = 0.1f;

    [DataField]
    public TimeSpan StuporTime = TimeSpan.FromSeconds(10f);

    [DataField]
    public EntProtoId StatusEffect = "StatusEffectFearStupor";
}
