using Content.Client.Stylesheets.Colorspace;
using Content.Client.Stylesheets.Palette;

namespace Content.Client._Scp.Stylesheets.Palette;

/// <summary>
/// Палитра цветов для SCP/Grimdark тематики.
/// </summary>
public static class ScpPalettes
{
    public static readonly Color PanelUltraDark = Color.FromHex("#090909");
    public static readonly Color PanelDarker = Color.FromHex("#121111");
    public static readonly Color PanelDark = Color.FromHex("#171616");
    public static readonly Color PanelLightDark = Color.FromHex("#373535");
    public static readonly Color LightGray = Color.FromHex("#b2b2b2");

    public static readonly Color BloodRed = Color.FromHex("#8B0000");
    public static readonly Color BloodRedDarker = Color.FromHex("#4D0000");
    public static readonly Color BloodRedDarker2 = Color.FromHex("#3A0000");
    public static readonly Color BloodRedHover = Color.FromHex("#5A0000");

    public static readonly Color SCPWhite = Color.FromHex("#e1e1e1");
    public static readonly Color ScpYellow = Color.FromHex("#F5DE5F");

    public static readonly Color GoodGreenFore = Color.FromHex("#1A4D1A");
    public static readonly Color GoodGreenHover = Color.FromHex("#2B662B");
    public static readonly Color GoodGreenDisabled = Color.FromHex("#0D260D");

    public static readonly Color DangerousRedFore = Color.FromHex("#660000");

    // Общие состояния кнопок
    public static readonly Color ButtonHover = SCPWhite.WithLightness(0.8f);
    public static readonly Color ButtonPressed = SCPWhite.WithLightness(0.65f);
    public static readonly Color ButtonDisabled = SCPWhite.WithLightness(0.4f);

    // --- Сборка палитр ---

    /// <summary>
    /// Основная темная палитра (Фоны, Окна, Стандартные кнопки).
    /// </summary>
    public static readonly ColorPalette Primary = new(
        Base: PanelDark,
        LightnessShift: 0,
        ChromaShift: 0,

        // Элементы управления (Кнопки)
        Element: PanelDarker,
        HoveredElement: ButtonHover,
        PressedElement: ButtonPressed,
        DisabledElement: ButtonDisabled,

        // Фон
        Background: PanelDark,            // Main
        BackgroundLight: PanelLightDark,  // Light
        BackgroundDark: PanelUltraDark,   // Dark

        // Текст
        Text: SCPWhite,
        TextDark: LightGray
    );

    /// <summary>
    /// Вторичная палитра (Инпуты, неактивные табы).
    /// </summary>
    public static readonly ColorPalette Secondary = Primary with
    {
        LightnessShift = 0.3f,
    };

    /// <summary>
    /// Палитра "Опасности" / Акцента (Красная).
    /// </summary>
    public static readonly ColorPalette Red = new(
        Base: BloodRed,
        LightnessShift: 0,
        ChromaShift: 0,

        Element: BloodRedDarker,    // ButtonColorDefaultRed
        HoveredElement: BloodRedHover,
        PressedElement: BloodRed,
        DisabledElement: BloodRedDarker2,

        Background: BloodRed,
        BackgroundLight: DangerousRedFore,
        BackgroundDark: BloodRedDarker2,

        Text: SCPWhite,
        TextDark: SCPWhite.WithAlpha(0.7f)
    );

    /// <summary>
    /// Палитра "Успеха" (Зеленая).
    /// </summary>
    public static readonly ColorPalette Green = new(
        Base: GoodGreenFore,
        LightnessShift: 0,
        ChromaShift: 0,

        Element: GoodGreenFore,
        HoveredElement: GoodGreenHover,
        PressedElement: GoodGreenFore.WithLightness(0.4f),
        DisabledElement: GoodGreenDisabled,

        Background: GoodGreenFore,
        BackgroundLight: Color.LimeGreen,
        BackgroundDark: GoodGreenDisabled,

        Text: SCPWhite,
        TextDark: SCPWhite.WithAlpha(0.7f)
    );
}
