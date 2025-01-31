using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

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
    public float Accumulator = 0;

    [AutoNetworkedField]
    public float BackroomsAccumulator = 0;

    [AutoNetworkedField]
    public float AnnouncementAccumulator = 600;

    [DataField("lifeEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string LifeEssenceCurrencyPrototype = "LifeEssence";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Essence = 0f;

    [DataField("scp106LifeEssenceAlert")]
    public ProtoId<AlertPrototype> Scp106EssenceAlert { get; set; } = "Scp106LifeEssence";

    [DataField("phantomAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PhantomAction = "Scp106BecomePhantom";

    public TimeSpan PhantomCoolDown = TimeSpan.FromSeconds(300);

    public float MaxScp106Portals = 5;
}
