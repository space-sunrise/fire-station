using Content.Server.EUI;
using Content.Shared._Scp.BodyTakeover;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Scp.BodyTakeover;

/// <summary>
/// Server-side EUI for the body takeover transfer window shown to the selected candidate.
/// </summary>
public sealed class BodyTakeoverTransferEui : BaseEui
{
    private readonly ICommonSession _session;
    private readonly string _entityName;
    private readonly Action<ICommonSession, BodyTakeoverTransferMessage.Choice> _onResponse;
    private readonly TimeSpan _transferDelay;

    public BodyTakeoverTransferEui(
        ICommonSession session,
        string entityName,
        Action<ICommonSession, BodyTakeoverTransferMessage.Choice> onResponse,
        TimeSpan transferDelay)
    {
        _session = session;
        _entityName = entityName;
        _onResponse = onResponse;
        _transferDelay = transferDelay;
    }

    public override EuiStateBase GetNewState()
    {
        return new BodyTakeoverTransferEuiState
        {
            EntityName = _entityName,
            TransferDelay = _transferDelay
        };
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not BodyTakeoverTransferMessage choice)
            return;

        _onResponse(_session, choice.Button);
        Close();
    }
}
