using Robust.Client.UserInterface.Controls;

namespace Content.Client._Scp.Scp173.UI;

public sealed class Scp173UiWidget : UIWidget
{
    private readonly Scp173ReagentBar _bar;

    public Scp173UiWidget()
    {
        _bar = new Scp173ReagentBar();
        AddChild(_bar);
    }

    public void SetReagentData(float current, float max, float bloatedMax)
    {
        _bar.UpdateInfo(current, max, bloatedMax);
    }

    public void SetSafeTimeData(TimeSpan time, TimeSpan? timeLeft)
    {
        _bar.UpdateSafeTimeInfo(time, timeLeft);
    }

    public void ToggleSafeTimeWindow(bool visible)
    {
        _bar.SafeTimeInfo.Visible = visible;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        RemoveAllChildren();
    }
}
