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
    [DataField] public bool IsContained;

    [AutoNetworkedField]
    public float AnnouncementAccumulator = 600;

    [DataField]
    public ProtoId<CurrencyPrototype> LifeEssenceCurrencyPrototype = "LifeEssence";

    [DataField, ViewVariables]
    [AutoNetworkedField]
    public FixedPoint2 Essence = 0f;

    [DataField]
    public ProtoId<AlertPrototype> Scp106EssenceAlert { get; set; } = "Scp106LifeEssence";

    [DataField]
    public EntProtoId PhantomAction = "Scp106BecomePhantom";

    public TimeSpan PhantomCoolDown = TimeSpan.FromSeconds(300);

    public float MaxScp106Portals = 3;

    public float Scp106HasPortals = 0;

    public bool HandTransformed = false;

    public EntityUid? Sword;
}
