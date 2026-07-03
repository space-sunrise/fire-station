using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio; // Sunrise-Add: поле звука уведомления о низком заряде

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Radio.Components;

public sealed partial class HeadsetComponent
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, bool> EnabledChannels = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, float> ChannelVolumes = new();

    [DataField]
    public float SendChargeCost = 2f; // Fire edit

    [DataField]
    public float ReceiveChargeCost = 1f; // Fire edit

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleHeadset";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField]
    public int ReceivedMessagesSinceLastNotify;

    [DataField]
    public SoundSpecifier LowBatteryNotifySound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/beeps.ogg");
}
