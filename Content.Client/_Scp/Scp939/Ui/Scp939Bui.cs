using Content.Client.VoiceMask;

namespace Content.Client._Scp.Scp939.Ui;

public sealed class Scp939Bui : VoiceMaskBoundUserInterface
{
    public Scp939Bui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            return;
        }

        _window.Title = Loc.GetString("scp939-window-title");
    }
}
