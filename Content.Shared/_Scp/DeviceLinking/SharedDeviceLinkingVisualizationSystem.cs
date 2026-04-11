using Robust.Shared.Serialization;

namespace Content.Shared._Scp.DeviceLinking;

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayToggledEvent : EntityEventArgs
{
    public readonly bool IsEnabled;

    public DeviceLinkOverlayToggledEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayData : EntityEventArgs
{
    public readonly List<DebugEntityConnectionData> Rays;

    public DeviceLinkOverlayData(List<DebugEntityConnectionData> rays)
    {
        Rays = rays;
    }
}

[Serializable, NetSerializable]
public readonly record struct DebugEntityConnectionData(NetEntity Source, List<NetEntity> Connections) { };
