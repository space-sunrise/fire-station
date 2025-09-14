using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp106.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp106Component : Component
{
    /// <summary>
    /// Если объект сдержан, он не должен иметь возможности юзать способки
    /// TODO: Возможно переместить в <see cref="ScpComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsContained;

    public TimeSpan TeleportationDuration = TimeSpan.FromSeconds(3.7f);

    [DataField]
    public ProtoId<CurrencyPrototype> LifeEssenceCurrencyPrototype = "LifeEssence";

    [DataField]
    public ProtoId<AlertPrototype> Scp106EssenceAlert= "Scp106LifeEssence";

    [DataField, ViewVariables, AutoNetworkedField]
    public FixedPoint2 Essence = 0f;

    #region Abilities

    [ViewVariables, AutoNetworkedField]
    public HashSet<FearState> AbsorbedFears = [];

    public TimeSpan NextEssenceAddedTime;

    [AutoNetworkedField]
    public TimeSpan PhantomCoolDown = TimeSpan.FromSeconds(300);

    [ViewVariables]
    public EntityUid? BladeEntity;

    #endregion
}
