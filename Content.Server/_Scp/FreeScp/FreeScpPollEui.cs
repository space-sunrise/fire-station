using Content.Server.EUI;
using Content.Shared._Scp.FreeScp;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Scp.FreeScp;

public sealed class FreeScpPollEui : BaseEui
{
    private readonly ICommonSession _session;
    private readonly Action<ICommonSession, bool> _onResponse;

    public FreeScpPollEui(ICommonSession session, Action<ICommonSession, bool> onResponse)
    {
        _session = session;
        _onResponse = onResponse;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not FreeScpPollMessage choice)
            return;

        _onResponse(_session, choice.Button == FreeScpPollMessage.Choice.Accept);
        Close();
    }
}
