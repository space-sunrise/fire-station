using Content.Client._Scp.Stylesheets.Palette;
using Content.Client.ContextMenu.UI;
using Content.Client.Examine;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls.FancyTree;
using Content.Client.Verbs.UI;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._Scp.Stylesheets.Stylesheets.Sheetlets;

/// <summary>
/// Переносит специфичные визуальные изменения SCP темы (Flat StyleBoxes, borders) из старого StyleNano.
/// </summary>
// TODO: Разбросать на разные шитлеты.
[CommonSheetlet]
public sealed class ScpThemeSheetlet : Sheetlet<NanotrasenStylesheet>
{
    /// <remarks>
    /// Этой штуке не подходит монохромный черно-белый стиль, ей нужны цвета(зеленый/красный).
    /// Поэтому это здесь.
    /// </remarks>>
    public const string StyleClassBreakerButton = "StyleClassBreakerButton";

    public const string StyleClassWindowHeadingBackground = "WindowHeadingBackground";
    public const string StyleClassFancyWindowTitle = "FancyWindowTitle";

    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        // Шрифты
        var notoSansDisplayBold14 = sheet.BaseFont.GetFont(14, FontKind.Bold);
        var notoSansDisplayBold16 = sheet.BaseFont.GetFont(16, FontKind.Bold);
        var notoSansMono = sheet.ResCache.GetFont("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf", 12);

        var font48 = sheet.BaseFont.GetFont(48, FontKind.Bold);

        // Текстуры слайдеров (нужны для перекраски)
        var sliderFillTex = sheet.GetTextureOr(new("slider_fill.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot);
        var sliderOutlineTex = sheet.GetTextureOr(new("slider_outline.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot);
        var sliderGrabTex = sheet.GetTextureOr(new("slider_grabber.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot);

        // --- Создание StyleBoxes (Fire edit logic) ---

        // Окно: Темный фон, Белая рамка
        var windowBackground = new StyleBoxFlat
        {
            BackgroundColor = ScpPalettes.PanelDark,
            BorderColor = ScpPalettes.SCPWhite,
        };
        windowBackground.SetContentMarginOverride(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);

        // Заголовок окна: Белый фон, Темная рамка
        var windowHeader = new StyleBoxFlat
        {
            ContentMarginBottomOverride = 0,
            BackgroundColor = ScpPalettes.SCPWhite,
            BorderColor = ScpPalettes.PanelDarker,
        };

        // Алерт заголовок
        var windowHeaderAlert = new StyleBoxFlat
        {
            ContentMarginBottomOverride = 0,
            BackgroundColor = ScpPalettes.SCPWhite,
            BorderColor = ScpPalettes.PanelDarker,
        };

        // Тултипы: Темнее фона, полупрозрачная белая рамка
        var tooltipBox = new StyleBoxFlat
        {
            BackgroundColor = ScpPalettes.PanelDarker,
            BorderColor = ScpPalettes.SCPWhite.WithAlpha(0.5f),
            BorderThickness = new Thickness(1f),
        };
        tooltipBox.SetContentMarginOverride(StyleBox.Margin.All, 2);
        tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        // TabContainer (Панель вкладок)
        var tabContainerPanel = new StyleBoxFlat
        {
            BackgroundColor = ScpPalettes.PanelDark,
        };
        tabContainerPanel.SetContentMarginOverride(StyleBox.Margin.All, 2);

        var tabContainerBoxActive = new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelDark };
        tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        var tabContainerBoxInactive = new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelLightDark };
        tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        // ItemList (Списки)
        var itemListItemBackground = new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelDark };
        itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        var itemListItemBackgroundDisabled = new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelDark };
        itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        var itemListBackgroundSelected = new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelDarker };
        itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        // Слайдеры
        var sliderFillBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            ExpandMarginLeft = -3,
            ExpandMarginTop = -3,
            ExpandMarginRight = -3,
            ExpandMarginBottom = -3,
            Modulate = ScpPalettes.SCPWhite,
        };
        sliderFillBox.SetPadding(StyleBox.Margin.Left, 2f);
        sliderFillBox.SetPatchMargin(StyleBox.Margin.All, 12);

        var sliderBackBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            Modulate = ScpPalettes.PanelDarker,
        };
        sliderBackBox.SetPatchMargin(StyleBox.Margin.All, 12);

        var sliderForeBox = new StyleBoxTexture
        {
            Texture = sliderOutlineTex,
            Modulate = ScpPalettes.PanelDarker,
        };
        sliderForeBox.SetPatchMargin(StyleBox.Margin.All, 12);

        var sliderGrabBox = new StyleBoxTexture
        {
            Texture = sliderGrabTex,
            Modulate = Color.Red,
        };
        sliderGrabBox.SetPatchMargin(StyleBox.Margin.All, 12);

        // Chat Panel
        var chatBg = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#313131"),
            BorderColor = ScpPalettes.SCPWhite,
        };

        // --- Формирование правил стилей ---

        return new StyleRule[]
        {
            // 1. Окна (Windows)
            // Фон окна
            Element()
                .Class(DefaultWindow.StyleClassWindowPanel)
                .Prop(PanelContainer.StylePropertyPanel, windowBackground),

            // Заголовок окна (Шапка)
            Element<PanelContainer>()
                .Class(DefaultWindow.StyleClassWindowHeader)
                .Prop(PanelContainer.StylePropertyPanel, windowHeader),

            Element<PanelContainer>()
                .Class(StyleClassWindowHeadingBackground)
                .Prop(PanelContainer.StylePropertyPanel, windowHeader),

            // Цвет текста заголовка (Темный на белом фоне)
            Element<Label>()
                .Class(DefaultWindow.StyleClassWindowTitle)
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker)
                .Prop(Label.StylePropertyFont, notoSansDisplayBold14),

            Element<Label>()
                .Class(StyleClassFancyWindowTitle)
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker)
                .Prop(Label.StylePropertyFont, notoSansDisplayBold14),

            Element<Label>()
                .Class("LabelHeadingTheBiggest")
                .Prop(Label.StylePropertyFontColor, ScpPalettes.SCPWhite)
                .Prop(Label.StylePropertyFont, font48),

            // Алерт заголовок (Красный режим)
            Element<PanelContainer>()
                .Class(StyleClass.AlertWindowHeader)
                .Prop(PanelContainer.StylePropertyPanel, windowHeaderAlert),

            Element<Label>()
                .Class("windowTitleAlert")
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker)
                .Prop(Label.StylePropertyFont, notoSansDisplayBold14),

            // Кнопка закрытия окна (крестик)
            Element<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDarker),

            Element<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelUltraDark),

            Element<TextureButton>()
                .Class(DefaultWindow.StyleClassWindowCloseButton)
                .PseudoPressed()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelUltraDark),

            // 2. Тултипы (Tooltips)
            Element<Tooltip>()
                .Prop(PanelContainer.StylePropertyPanel, tooltipBox),

            Element<PanelContainer>()
                .Class(StyleClass.TooltipPanel)
                .Prop(PanelContainer.StylePropertyPanel, tooltipBox),

            Element<PanelContainer>()
                .Class(ExamineSystem.StyleClassEntityTooltip)
                .Prop(PanelContainer.StylePropertyPanel, tooltipBox),

            // 3. Слайдеры (Sliders)
            Element<Slider>()
                .Prop(Slider.StylePropertyBackground, sliderBackBox)
                .Prop(Slider.StylePropertyForeground, sliderForeBox)
                .Prop(Slider.StylePropertyGrabber, sliderGrabBox)
                .Prop(Slider.StylePropertyFill, sliderFillBox),

            // 4. TabContainer (Вкладки)
            Element<TabContainer>()
                .Prop(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel)
                .Prop(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive),

            // 5. Чат (Chat)
            Element<PanelContainer>()
                .Class("ChatPanel")
                .Prop(PanelContainer.StylePropertyPanel, chatBg),

            // 6. Списки (ItemList)
            Element<ItemList>()
                .Prop(ItemList.StylePropertyBackground, new StyleBoxFlat { BackgroundColor = ScpPalettes.PanelDark })
                .Prop(ItemList.StylePropertyItemBackground, itemListItemBackground)
                .Prop(ItemList.StylePropertyDisabledItemBackground, itemListItemBackgroundDisabled)
                .Prop(ItemList.StylePropertySelectedItemBackground, itemListBackgroundSelected),

            // 7. Кнопки (Buttons) - Переопределение цветов состояний (Hover/Pressed)
            // Стандартная кнопка
            Element<ContainerButton>()
                .Class(ContainerButton.StyleClassButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.HoveredElement),

            Element<ContainerButton>()
                .Class(ContainerButton.StyleClassButton)
                .PseudoPressed()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.PressedElement),

            Element<ContainerButton>()
                .Class(ContainerButton.StyleClassButton)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.DisabledElement),

            // Цвет текста при наведении (Label внутри кнопок)
            Child()
                .Parent(Element<ContainerButton>().PseudoHovered())
                .Child(Element<Label>())
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker),

            Child()
                .Parent(Element<ContainerButton>().PseudoHovered())
                .Child(Element<RichTextLabel>())
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDarker),

            Child()
                .Parent(Element<ContainerButton>().PseudoPressed())
                .Child(Element<Label>())
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker),

            Child()
                .Parent(Element<ContainerButton>().PseudoPressed())
                .Child(Element<RichTextLabel>())
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDarker),

            // Context Menu (ПКМ)
            Element<ContextMenuElement>()
                .Class(ContextMenuElement.StyleClassContextMenuButton)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.Background),

            Element<ContextMenuElement>()
                .Class(ContextMenuElement.StyleClassContextMenuButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.HoveredElement),

            Element<ContextMenuElement>()
                .Class(ContextMenuElement.StyleClassContextMenuButton)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.DisabledElement),

            Child()
                .Parent(Element<ContextMenuElement>().PseudoHovered())
                .Child(Element<Label>())
                .Prop(Label.StylePropertyFontColor, ScpPalettes.PanelDarker),

            // Еще одно
            Element<ConfirmationMenuElement>()
                .Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.Background),

            Element<ConfirmationMenuElement>()
                .Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.HoveredElement),

            Element<ConfirmationMenuElement>()
                .Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.DisabledElement),

            // ListContainer Button
            Element<ContainerButton>()
                .Class(ListContainer.StyleClassListContainerButton)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.Element),

            Element<ContainerButton>()
                .Class(ListContainer.StyleClassListContainerButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.HoveredElement),

            Element<ContainerButton>()
                .Class(ListContainer.StyleClassListContainerButton)
                .PseudoPressed()
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.Primary.HoveredElement),

            // NOTE: ListContainerButton font color is handled programmatically in PlayerListEntry.xaml.cs
            // because CSS ParentOf rules don't re-evaluate when parent pseudo-class changes

            // 8. LineEdit (Поля ввода)
            Element<LineEdit>()
                .Prop(LineEdit.StylePropertySelectionColor, ScpPalettes.SCPWhite.WithAlpha(0.25f))
                .Prop(LineEdit.StylePropertyCursorColor, ScpPalettes.BloodRed)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelLightDark.WithAlpha(0.8f)),

            // 9. Разное (Misc)
            // Моноширинный шрифт
            Element()
                .Class(StyleClass.Monospace)
                .Prop("font", notoSansMono),

            // Fancy Tree
            Element<ContainerButton>()
                .Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassEvenRow)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#1A1A1A") }),

            Element<ContainerButton>()
                .Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassOddRow)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#1A1A1A") * new Color(0.8f, 0.8f, 0.8f) }),

            Element<ContainerButton>()
                .Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassSelected)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = new Color(40, 0, 0) }),

            // Горячий слот (Hotbar) цифра
            Element<RichTextLabel>()
                .Class("hotbarSlotNumber")
                .Prop("font", notoSansDisplayBold16),

            // Специальные бэкграунды (из StyleNano)
            Element<PanelContainer>()
                .Class("PanelBackgroundBaseDark")
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDark),

            Element<PanelContainer>()
                .Class(StyleClass.BackgroundPanel)
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDark),

            Element<SplitContainer>()
                .Class("BackgroundDark")
                .Prop(Control.StylePropertyModulateSelf, ScpPalettes.PanelDarker),

            // BoxContainer / ScrollContainer (Модуляция фона)
            Element<BoxContainer>()
                .Prop(Control.StylePropertyModulateSelf,
                    new StyleBoxFlat
                {
                    BackgroundColor = ScpPalettes.PanelDark,
                    BorderColor = ScpPalettes.SCPWhite,
                }),

            Element<ScrollContainer>()
                 .Prop(Control.StylePropertyModulateSelf,
                     new StyleBoxFlat
                 {
                     BackgroundColor = ScpPalettes.PanelDark,
                     BorderColor = ScpPalettes.SCPWhite,
                 }),

            // ЛКП
            Element<ContainerButton>()
                .Class(StyleClassBreakerButton)
                .Modulate(ScpPalettes.Red.Element),

            Element<ContainerButton>()
                .Class(StyleClassBreakerButton)
                .PseudoPressed()
                .Modulate(ScpPalettes.Green.Element),

            Element<ContainerButton>()
                .Class(StyleClassBreakerButton)
                .PseudoHovered()
                .Modulate(ScpPalettes.Red.HoveredElement),

            Element<ContainerButton>()
                .Class(StyleClassBreakerButton)
                .PseudoDisabled()
                .Modulate(ScpPalettes.Red.DisabledElement),

            Child()
                .Parent(Element<ContainerButton>().Class(StyleClassBreakerButton))
                .Child(Element<Label>())
                .FontColor(ScpPalettes.SCPWhite),
        };
    }
}
