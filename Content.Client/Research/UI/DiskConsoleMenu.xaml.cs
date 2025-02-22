using Content.Client.UserInterface.Controls;
using Content.Shared.Research;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Research.UI;

[GenerateTypedNameReferences]
public sealed partial class DiskConsoleMenu : FancyWindow
{
    public event Action? OnServerButtonPressed;
    public event Action? OnPrintButtonPressed;

    private IPrototypeManager _prototypeManager;

    public DiskConsoleMenu(IPrototypeManager prototypeManager)
    {
        RobustXamlLoader.Load(this);

        _prototypeManager = prototypeManager;

        ServerButton.OnPressed += _ => OnServerButtonPressed?.Invoke();
        PrintButton.OnPressed += _ => OnPrintButtonPressed?.Invoke();
    }

    public void Update(DiskConsoleBoundUserInterfaceState state)
    {
        PrintButton.Disabled = !state.CanPrint;
        TotalLabel.Text = Loc.GetString("tech-disk-ui-total-label", ("amount", ToPrettyString(state.ServerPoints)));
        CostLabel.Text = Loc.GetString("tech-disk-ui-cost-label", ("amount", ToPrettyString(state.PointCost)));
    }


    private string ToPrettyString(Dictionary<ProtoId<ResearchPointPrototype>, int> data)
    {
        var prettyString = string.Empty;

        foreach (var (pointType, value) in data)
        {
            var prototype = _prototypeManager.Index<ResearchPointPrototype>(pointType);
            prettyString += $"{Loc.GetString(prototype.Name)}: {value}  ";
        }

        return prettyString;
    }
}

