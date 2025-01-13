using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Scp.Scp106.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp106Component : Component
{
    /// <summary>
    /// Если объект сдержан, он не должен иметь возможности юзать способки
    /// TODO: Возможно переместить в <see cref="ScpComponent"/>
    /// </summary>
    [DataField] public bool IsContained;

    [DataField, AutoNetworkedField]
    public int AmoutOfPhantoms = 0;

    [DataField, AutoNetworkedField]
    public int AmountOfCorporealPhantoms = 0;

    [AutoNetworkedField]
    public float Accumulator = 0;

    [DataField("lifeEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string LifeEssenceCurrencyPrototype = "LifeEssence";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Essence = 0f;
}
