using Robust.Client.UserInterface.Controls;

namespace Content.Client._Scp.Scp096.Ui;

public sealed class Scp096UiWidget : UIWidget
{
    private readonly Label _timeLabel;
    private readonly Label _targetsLabel;
    public Scp096UiWidget()
    {
        _timeLabel = new Label();
        _timeLabel.Align = Label.AlignMode.Center;
        _timeLabel.HorizontalExpand = true;

        _targetsLabel = new Label();
        _targetsLabel.Align = Label.AlignMode.Center;
        _targetsLabel.HorizontalExpand = true;


        var boxContainer = new BoxContainer();
        boxContainer.Orientation = LayoutOrientation.Vertical;
        boxContainer.HorizontalExpand = true;
        boxContainer.VerticalExpand = true;

        AddChild(boxContainer);

        boxContainer.AddChild(_timeLabel);
        boxContainer.AddChild(_targetsLabel);

        SetWidth = 100;
        SetHeight = 100;
    }

    public void SetData(double timeLeft, double targets)
    {
        var timeLeftText = $"{timeLeft:F2}";

        _timeLabel.Text = Loc.GetString("scp096-time-left-label", ("time", timeLeftText));
        _targetsLabel.Text = Loc.GetString("scp096-targets-left-label", ("targets", targets));
    }
}
