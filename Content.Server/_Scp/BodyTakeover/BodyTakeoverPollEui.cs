using Content.Server.EUI;
using Content.Shared._Scp.BodyTakeover;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Scp.BodyTakeover;

/// <summary>
/// Server-side EUI for the body takeover poll window.
/// </summary>
public sealed class BodyTakeoverPollEui : BaseEui
{
    private readonly ICommonSession _session;
    private readonly string _entityName;
    private readonly Action<ICommonSession, bool> _onResponse;

    public BodyTakeoverPollEui(ICommonSession session, string entityName, Action<ICommonSession, bool> onResponse)
    {
        _session = session;
        _entityName = entityName;
        _onResponse = onResponse;
    }

    public override EuiStateBase GetNewState()
    {
        return new BodyTakeoverPollEuiState { EntityName = _entityName };
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not BodyTakeoverPollMessage choice)
            return;

        _onResponse(_session, choice.Button == BodyTakeoverPollMessage.Choice.Accept);
        Close();
    }
}
