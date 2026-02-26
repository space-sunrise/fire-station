using Content.Client._Scp.UI.Compatibility;
using Robust.Client.UserInterface.Controls;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class LobbyGui
{
    /// <summary>
    /// Регистрирует кнопку для инвертирования цвета содержимого при наведении/нажатии.
    /// Вызывать после <see cref="SetupButtonIcon"/> для каждой кнопки с иконкой.
    /// </summary>
    private static void TrackButtonHover(Button button)
    {
        HoverColorHelper.TrackButtonHover(button);
    }
}
