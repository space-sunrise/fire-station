using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.BodyTakeover;

[Serializable, NetSerializable]
public sealed class BodyTakeoverPollMessage : EuiMessageBase
{
    public enum Choice { Accept, Decline }
    public Choice Button;
}

[Serializable, NetSerializable]
public sealed class BodyTakeoverTransferMessage : EuiMessageBase
{
    public enum Choice { Now, Wait, Decline }
    public Choice Button;
}

[Serializable, NetSerializable]
public sealed class BodyTakeoverTransferEuiState : EuiStateBase
{
    public string EntityName = string.Empty;
    public TimeSpan TransferDelay;
}

[Serializable, NetSerializable]
public sealed class BodyTakeoverPollEuiState : EuiStateBase
{
    public string EntityName = string.Empty;
}
