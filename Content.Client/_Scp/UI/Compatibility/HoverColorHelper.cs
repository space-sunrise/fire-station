using Content.Client._Scp.Stylesheets.Palette;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Scp.UI.Compatibility;

/// <summary>
/// Статический хелпер для инвертирования цвета содержимого UI-элементов при наведении/нажатии.
/// <para/>
/// Из-за ограничений RobustToolbox, стили не поддерживают изменение цвета дочерних элементов
/// при смене состояния кнопки (hover, pressed и т.д.). Этот класс предоставляет
/// унифицированные методы для обхода этого ограничения.
/// </summary>
public static class HoverColorHelper
{
    /// <summary>
    /// Цвет содержимого в нормальном состоянии (белый на тёмном фоне).
    /// </summary>
    public static readonly Color NormalColor = ScpPalettes.SCPWhite;

    /// <summary>
    /// Цвет содержимого при наведении/нажатии (чёрный на белом фоне).
    /// </summary>
    public static readonly Color InvertedColor = ScpPalettes.PanelDarker;

    /// <summary>
    /// Определяет нужный цвет содержимого на основе текущего состояния кнопки (DrawMode).
    /// <para/>
    /// Если кнопка отключена — возвращает нормальный цвет.
    /// Если DrawMode равен Pressed или Hover (белый фон) — возвращает инвертированный цвет.
    /// В остальных случаях — нормальный цвет.
    /// </summary>
    public static Color GetColorForCurrentState(BaseButton button)
    {
        if (button.Disabled)
            return NormalColor;

        var needsInverted = button.DrawMode is BaseButton.DrawModeEnum.Pressed or BaseButton.DrawModeEnum.Hover;
        return needsInverted ? InvertedColor : NormalColor;
    }

    /// <summary>
    /// Определяет нужный цвет содержимого при наведении курсора на кнопку.
    /// <para/>
    /// Если кнопка отключена — нормальный цвет (фон остаётся тёмным).
    /// Иначе — инвертированный цвет (фон станет белым).
    /// </summary>
    public static Color GetColorForMouseEnter(BaseButton button)
    {
        return button.Disabled ? NormalColor : InvertedColor;
    }

    /// <summary>
    /// Определяет нужный цвет содержимого при уходе курсора с кнопки.
    /// <para/>
    /// ВАЖНО: событие OnMouseExited срабатывает ДО того, как BaseButton
    /// сбросит внутренний флаг _beingHovered. Поэтому IsHovered и DrawMode
    /// ещё показывают состояние Hover. Мы предсказываем будущее состояние:
    /// <list type="bullet">
    /// <item>Если Pressed=true → кнопка останется нажатой → белый фон → инвертированный цвет</item>
    /// <item>Если Pressed=false → кнопка вернётся в Normal → тёмный фон → нормальный цвет</item>
    /// </list>
    /// </summary>
    public static Color GetColorForMouseExit(BaseButton button)
    {
        if (button.Disabled)
            return NormalColor;

        return button.Pressed ? InvertedColor : NormalColor;
    }

    /// <summary>
    /// Рекурсивно устанавливает <see cref="Control.ModulateSelfOverride"/> для всех дочерних
    /// элементов, являющихся текстовыми или графическими (RichTextLabel, Label, TextureRect, TextureButton).
    /// <para/>
    /// Обходит всё дерево дочерних элементов от указанного корня вглубь.
    /// </summary>
    /// <param name="root">Корневой элемент, с которого начинается обход потомков.</param>
    /// <param name="color">Цвет, который будет установлен подходящим дочерним элементам.</param>
    public static void SetContentColor(Control root, Color color)
    {
        foreach (var child in root.Children)
        {
            if (child is RichTextLabel or Label or TextureRect or TextureButton)
                child.ModulateSelfOverride = color;

            SetContentColor(child, color);
        }
    }

    /// <summary>
    /// Подписывает любую <see cref="BaseButton"/> на события наведения/нажатия
    /// и автоматически инвертирует цвет её дочерних элементов.
    /// <para/>
    /// Используйте этот метод для кнопок, к которым нельзя добавить partial-класс
    /// или переопределить DrawModeChanged (например, <c>OptionButton</c> из движка).
    /// </summary>
    /// <param name="button">Кнопка, за которой нужно следить.</param>
    public static void TrackButtonHover(BaseButton button)
    {
        button.OnMouseEntered += _ => SetContentColor(button, GetColorForMouseEnter(button));
        button.OnMouseExited += _ => SetContentColor(button, GetColorForMouseExit(button));
        button.OnPressed += _ => SetContentColor(button, GetColorForCurrentState(button));
        button.OnToggled += _ => SetContentColor(button, GetColorForCurrentState(button));

        // Установить начальный цвет
        SetContentColor(button, GetColorForCurrentState(button));
    }
}
