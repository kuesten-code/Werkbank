using MudBlazor;

namespace Kuestencode.Rapport.Shared;

public static class KuestenCodeTheme
{
    public static MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F2A3D",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#081820",
            PrimaryLighten = "#1A3D55",

            Secondary = "#2E3440",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#1E2430",
            SecondaryLighten = "#3E4450",

            Tertiary = "#3FA796",
            TertiaryContrastText = "#FFFFFF",

            Background = "#F4F6F8",
            BackgroundGray = "#E8ECEF",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1A1A1A",
            AppbarBackground = "#0F2A3D",
            AppbarText = "#FFFFFF",

            TextPrimary = "#1A1A1A",
            TextSecondary = "#6B7280",
            TextDisabled = "#9CA3AF",

            Warning = "#D1A93B",
            WarningContrastText = "#1A1A1A",
            WarningDarken = "#B8942F",
            WarningLighten = "#D9B653",

            Error = "#C2413A",
            ErrorContrastText = "#FFFFFF",
            ErrorDarken = "#A3352F",
            ErrorLighten = "#CF5D56",

            Success = "#3FA796",
            SuccessContrastText = "#FFFFFF",
            SuccessDarken = "#358C7E",
            SuccessLighten = "#5BB5A6",

            Info = "#1A3D55",
            InfoContrastText = "#FFFFFF",
            InfoDarken = "#0F2A3D",
            InfoLighten = "#2A4D65",

            Divider = "#E5E7EB",
            DividerLight = "#F3F4F6",

            ActionDefault = "#6B7280",
            ActionDisabled = "#9CA3AF",
            ActionDisabledBackground = "#E5E7EB",

            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#4A9FD8",
            PrimaryContrastText = "#000000",
            PrimaryDarken = "#2A7DB8",
            PrimaryLighten = "#6AB5E0",

            Secondary = "#5E6470",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#3E4450",
            SecondaryLighten = "#7E8490",

            Tertiary = "#6BC5B5",
            TertiaryContrastText = "#000000",

            Background = "#121212",
            BackgroundGray = "#1E1E1E",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
            DrawerText = "#E5E7EB",
            AppbarBackground = "#0F2A3D",
            AppbarText = "#FFFFFF",

            TextPrimary = "#FFFFFF",
            TextSecondary = "#B8BDC5",
            TextDisabled = "#757575",

            Warning = "#E5C56F",
            WarningContrastText = "#000000",

            Error = "#E57373",
            ErrorContrastText = "#000000",

            Success = "#6BC5B5",
            SuccessContrastText = "#000000",

            Info = "#4A9FD8",
            InfoContrastText = "#000000",

            Divider = "#3A3A3A",
            DividerLight = "#2A2A2A",

            ActionDefault = "#B8BDC5",
            ActionDisabled = "#757575",
            ActionDisabledBackground = "#2A2A2A",

            HoverOpacity = 0.10,
            RippleOpacity = 0.15,
        },

        LayoutProperties = new LayoutProperties
        {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px"
        },

        ZIndex = new ZIndex
        {
            Drawer = 1300,
            AppBar = 1200,
            Dialog = 1400,
            Popover = 1500,
            Snackbar = 1600,
            Tooltip = 1700
        }
    };
}
