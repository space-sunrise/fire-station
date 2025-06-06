using Content.Client.Anomaly.Ui;
using Content.Shared._Scp.Scp914;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp914.Ui;

public sealed class Scp914BoundUserInterface : BoundUserInterface
{
    private Scp914Window? _window;
    private IGameTiming _gameTiming;

    public Scp914BoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

        _gameTiming = IoCManager.Resolve<IGameTiming>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new Scp914Window(_gameTiming);
        _window.OnClose += Close;
        _window.OnNewModeSelected += OnNewModeSelected;
        _window.OnStartCycle += OnStartCycle;

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();
    }

    private void OnStartCycle()
    {
        var startCycleMessage = new Scp914StartCycleMessage();
        SendPredictedMessage(startCycleMessage);
    }

    private void OnNewModeSelected(Scp914CycleDirection direction)
    {
        var changeModeMessage = new Scp914ChangeModeRequestMessage(direction);
        SendPredictedMessage(changeModeMessage);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not Scp914BuiState newState)
        {
            return;
        }

        _window?.UpdateState(newState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_window == null)
        {
            return;
        }

        _window.OnClose -= Close;
        _window.OnNewModeSelected -= OnNewModeSelected;

        _window.Dispose();
        _window = null;
    }
}
