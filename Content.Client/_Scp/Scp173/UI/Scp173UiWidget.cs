using Content.Client._Scp.SafeTime;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Scp.Scp173.UI;

public sealed class Scp173UiWidget : UIWidget
{
    public readonly Scp173ReagentBar ReagentBar;
    public readonly SafeTimeProgressBar SafeTime;

    public Scp173UiWidget()
    {
        ReagentBar = new ();
        SafeTime = new();

        Orientation = LayoutOrientation.Vertical;
        AddChild(ReagentBar);
        AddChild(SafeTime);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        RemoveAllChildren();
        Orphan();
    }
}
