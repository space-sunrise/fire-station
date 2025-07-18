﻿using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Scp.Fear.UI;

[GenerateTypedNameReferences]
public sealed partial class FearInfo : Control
{
    public string PhobiaNameString
    {
        set => PhobiaName.Text = Loc.GetString(value);
    }

    public string Description
    {
        set => PhobiaDescription.Text = Loc.GetString(value);
    }

    public Color? Color
    {
        set
        {
            if (value == null)
            {
                ColoredLine.Visible = false;
                return;
            }

            if (ColoredLine.PanelOverride is not StyleBoxFlat panel)
            {
                ColoredLine.Visible = false;
                return;
            }

            panel.BackgroundColor = value.Value;
            ColoredLine.Visible = true;
        }
    }

    public FearInfo()
    {
        RobustXamlLoader.Load(this);
    }
}
