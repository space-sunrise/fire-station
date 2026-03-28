using Content.Server.EUI;
using Content.Shared._Scp.FreeScp;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Scp.FreeScp;

public sealed class FreeScpTransferEui : BaseEui
{
    private readonly ICommonSession _session;
    private readonly string _scpName;
    private readonly Action<ICommonSession, FreeScpTransferMessage.Choice> _onResponse;

    public FreeScpTransferEui(ICommonSession session, string scpName, Action<ICommonSession, FreeScpTransferMessage.Choice> onResponse)
    {
        _session = session;
        _scpName = scpName;
        _onResponse = onResponse;
    }

    public override EuiStateBase GetNewState()
    {
        return new FreeScpTransferEuiState { ScpName = _scpName };
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not FreeScpTransferMessage choice)
            return;

        _onResponse(_session, choice.Button);
        Close();
    }
}
