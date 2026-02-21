using Content.Client._Scp.UI.Compatibility;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lathe.UI;

public sealed partial class RecipeControl
{
    private Control? _parentButton;

    protected override void EnteredTree()
    {
        base.EnteredTree();
        InitializeHoverHandling();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        CleanupHoverHandling();
    }

    /// <summary>
    /// Вызывается при добавлении в дерево для настройки обработки наведения.
    /// </summary>
    private void InitializeHoverHandling()
    {
        _parentButton = Button;
        SubscribeToButtonEvents();
        UpdateTextColor();
    }

    /// <summary>
    /// Вызывается при удалении из дерева для очистки обработки наведения.
    /// </summary>
    private void CleanupHoverHandling()
    {
        UnsubscribeFromButtonEvents();
    }

    /// <summary>
    /// Вызывается при изменении состояния Disabled для обновления цвета текста.
    /// </summary>
    public void RefreshTextColor()
    {
        UpdateTextColor();
    }

    private void SubscribeToButtonEvents()
    {
        if (_parentButton == null)
            return;

        _parentButton.OnMouseEntered += OnParentMouseEntered;
        _parentButton.OnMouseExited += OnParentMouseExited;
    }

    private void UnsubscribeFromButtonEvents()
    {
        if (_parentButton == null)
            return;

        _parentButton.OnMouseEntered -= OnParentMouseEntered;
        _parentButton.OnMouseExited -= OnParentMouseExited;
        _parentButton = null;
    }

    private void OnParentMouseEntered(GUIMouseHoverEventArgs args)
    {
        RecipeName.FontColorOverride = HoverColorHelper.GetColorForMouseEnter(Button);
    }

    private void OnParentMouseExited(GUIMouseHoverEventArgs args)
    {
        RecipeName.FontColorOverride = HoverColorHelper.GetColorForMouseExit(Button);
    }

    private void UpdateTextColor()
    {
        RecipeName.FontColorOverride = HoverColorHelper.GetColorForCurrentState(Button);
    }
}
