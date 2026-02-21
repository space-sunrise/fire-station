using Content.Client._Scp.UI.Compatibility;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

// ReSharper disable once CheckNamespace
namespace Content.Client.UserInterface.Controls;

public sealed partial class ListContainerButton
{
    private Control? _trackedElement;

    #region Public fields

    public bool HoverExtenstionEnabled
    {
        get;
        set
        {
            field = value;
            if (ShouldHandling())
                InitializeHoverHandling(false);
            else
                CleanupHoverHandling();
        }
    } = true;

    public HashSet<string> NameBlacklist
    {
        get;
        set
        {
            field = value;
            if (ShouldHandling())
                InitializeHoverHandling(false);
            else
                CleanupHoverHandling();
        }
    } = [];

    #endregion

    #region Enter&Exit

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

    #endregion

    #region Restrictions

    private bool ShouldHandling(Control? control = null)
    {
        if (!HoverExtenstionEnabled)
            return false;

        if (!IsBlacklistPassRecursive(control))
            return false;

        return true;
    }

    private bool IsBlacklistPassRecursive(Control? control = null)
    {
        if (NameBlacklist.Count == 0)
            return true;

        if (control == null)
            return true;

        if (control.Name != null && NameBlacklist.Contains(control.Name))
            return false;

        return IsBlacklistPassRecursive(control.Parent);
    }

    #endregion

    #region Initialize&Shutdown

    /// <summary>
    /// Вызывается при добавлении в дерево для настройки обработки наведения.
    /// </summary>
    private void InitializeHoverHandling(bool checkRestrictions = true)
    {
        if (checkRestrictions && !ShouldHandling(this))
            return;

        _trackedElement = this;
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

    private void SubscribeToButtonEvents()
    {
        if (_trackedElement == null)
            return;

        _trackedElement.OnMouseEntered += OnParentMouseEntered;
        _trackedElement.OnMouseExited += OnParentMouseExited;
    }

    private void UnsubscribeFromButtonEvents()
    {
        if (_trackedElement == null)
            return;

        _trackedElement.OnMouseEntered -= OnParentMouseEntered;
        _trackedElement.OnMouseExited -= OnParentMouseExited;
        _trackedElement = null;
    }

    #endregion

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();

        UpdateTextColor();
    }

    private void OnParentMouseEntered(GUIMouseHoverEventArgs args)
    {
        if (_trackedElement is not BaseButton button)
            return;

        HoverColorHelper.SetContentColor(this, HoverColorHelper.GetColorForMouseEnter(button));
    }

    private void OnParentMouseExited(GUIMouseHoverEventArgs args)
    {
        if (_trackedElement is not BaseButton button)
            return;

        HoverColorHelper.SetContentColor(this, HoverColorHelper.GetColorForMouseExit(button));
    }

    private void UpdateTextColor()
    {
        if (_trackedElement is not BaseButton button)
            return;

        HoverColorHelper.SetContentColor(this, HoverColorHelper.GetColorForCurrentState(button));
    }
}
