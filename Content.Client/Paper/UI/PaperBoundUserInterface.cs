using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    private PaperKnowledgeHighlightMessage? _highlightMessage; // Fire added - cache server-provided knowledge highlights between UI messages

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        // Fire added start - reuse the server-provided highlighted paper text when it still matches the current state
        var paperState = (PaperBoundUserInterfaceState) state;
        string? highlightedText = null;

        if (_highlightMessage != null && paperState.Text == _highlightMessage.RawText)
            highlightedText = _highlightMessage.HighlightedText;
        else
            _highlightMessage = null;
        // Fire added end

        _window?.Populate(paperState, highlightedText); // Fire edit - pass highlighted paper text for knowledge hint rendering
    }

    // Fire added start - receive out-of-band paper knowledge highlight updates without waiting for a full state resend
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is not PaperKnowledgeHighlightMessage highlight)
            return;

        _highlightMessage = highlight;

        if (State is PaperBoundUserInterfaceState state)
            _window?.Populate(state, state.Text == highlight.RawText ? highlight.HighlightedText : null);
    }
    // Fire added end

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }
}
