using Content.Shared.Storage.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp914;


[RegisterComponent, NetworkedComponent]
public sealed partial class Scp914Component : Component
{
    public Scp914Mode CurrentMode { get; set; } = Scp914Mode.Rough;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Active { get; set; }

    [DataField]
    public TimeSpan UpgradeDuration { get; private set; } = TimeSpan.FromSeconds(15);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpgradeTimeEnd { get; set; } = TimeSpan.MinValue;

    [DataField]
    public TimeSpan UpgradeCooldown { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan NextUpgradeTime { get; set; }

    [DataField]
    public TimeSpan CycleCooldown = TimeSpan.FromSeconds(1);

    public TimeSpan NextChangeCycleTime { get; set; } = TimeSpan.MinValue;

    [ViewVariables]
    public EntityUid InputContainer { get; set; }

    [ViewVariables]
    public EntityUid OutputContainer { get; set; }

    [DataField]
    public SoundSpecifier RefineSound { get; private set; } = new SoundPathSpecifier("/Audio/_Scp/Scp914/scp914_refine.ogg");

    [DataField]
    public SoundSpecifier ClackSound { get; private set; } = new SoundPathSpecifier("/Audio/_Scp/Scp914/scp914_clack.ogg");
}

[RegisterComponent]
public sealed partial class Scp914ContainerComponent : Component
{
    [DataField(required: true)]
    public Scp914ContainerType ContainerType { get; set; }
}

[RegisterComponent]
public sealed partial class Scp914UpgradableComponent : Component
{
    [DataField(required: true)]
    public Dictionary<Scp914Mode, List<UpgradeOption>> UpgradeOptions { get; private set; } = new();
}

[Serializable, NetSerializable]
public enum Scp914Mode : byte
{
    Rough = 0,
    Coarse = 1,
    OneToOne = 2,
    Fine = 4,
    VeryFine = 8,
}

[Serializable]
public enum Scp914ContainerType : byte
{
    Input = 0,
    Output = 1
}

[DataDefinition]
public sealed partial class UpgradeOption
{
    [DataField(required: true)]
    public EntProtoId? Item { get; private set; }

    [DataField]
    public float Chance { get; private set; } = 1.0f;
}
