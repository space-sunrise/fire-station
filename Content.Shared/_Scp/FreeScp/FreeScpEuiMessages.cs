using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.FreeScp;

[Serializable, NetSerializable]
public sealed class FreeScpPollMessage : EuiMessageBase
{
    public enum Choice { Accept, Decline }
    public Choice Button;
}

[Serializable, NetSerializable]
public sealed class FreeScpTransferMessage : EuiMessageBase
{
    public enum Choice { Now, Wait, Decline }
    public Choice Button;
}

[Serializable, NetSerializable]
public sealed class FreeScpTransferEuiState : EuiStateBase
{
    public string ScpName = string.Empty;
}
