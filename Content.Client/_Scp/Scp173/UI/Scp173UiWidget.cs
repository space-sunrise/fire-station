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

    public void SetData(float current, float max, float bloatedMax)
    {
        _bar.UpdateInfo(current, max, bloatedMax);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        RemoveAllChildren();
    }
}
