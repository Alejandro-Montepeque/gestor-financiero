using MudBlazor;

namespace GestorFinanciero.Web.Theming;

/// <summary>
/// Central MudBlazor theme for the whole app. Two palettes: a dark one that
/// ships as the default (matches the portfolio site) and a light one for users
/// who prefer it. Semantic colors — Success for income, Error for expenses —
/// are used consistently everywhere.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary        = "#7C3AED",   // Violet (matches portfolio)
            Secondary      = "#22D3EE",   // Cyan accent
            Tertiary       = "#EC4899",   // Pink accent
            Success        = "#10B981",   // Green — income
            Error          = "#EF4444",   // Red — expenses
            Warning        = "#F59E0B",   // Amber — alerts
            Info           = "#3B82F6",   // Blue — informational
            Background     = "#FAFAF9",
            Surface        = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText     = "#111827",
            DrawerBackground = "#F5F5F4",
            DrawerText     = "#111827",
            DrawerIcon     = "#4B5563",
            TextPrimary    = "#111827",
            TextSecondary  = "#4B5563",
            ActionDefault  = "#4B5563",
            LinesDefault   = "#E5E7EB",
            LinesInputs    = "#D1D5DB",
        },
        PaletteDark = new PaletteDark
        {
            Primary        = "#A78BFA",   // Lighter violet in dark mode for better contrast
            Secondary      = "#22D3EE",
            Tertiary       = "#F472B6",
            Success        = "#34D399",
            Error          = "#F87171",
            Warning        = "#FBBF24",
            Info           = "#60A5FA",
            Black          = "#0A0A0F",
            Background     = "#0A0A0F",   // Portfolio-matching background
            BackgroundGray = "#0F0F17",
            Surface        = "#161622",   // Card background
            AppbarBackground = "#0F0F17",
            AppbarText     = "#F3F4F6",
            DrawerBackground = "#0F0F17",
            DrawerText     = "#E5E7EB",
            DrawerIcon     = "#9CA3AF",
            TextPrimary    = "#F3F4F6",
            TextSecondary  = "#9CA3AF",
            TextDisabled   = "rgba(243,244,246, 0.4)",
            ActionDefault  = "#9CA3AF",
            ActionDisabled = "rgba(243,244,246, 0.3)",
            ActionDisabledBackground = "rgba(243,244,246, 0.1)",
            LinesDefault   = "rgba(243,244,246, 0.12)",
            LinesInputs    = "rgba(243,244,246, 0.2)",
            TableLines     = "rgba(243,244,246, 0.12)",
            Divider        = "rgba(243,244,246, 0.12)",
            OverlayLight   = "rgba(0,0,0, 0.5)",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "ui-sans-serif", "system-ui", "sans-serif" },
                FontSize = "0.9375rem",
                LineHeight = "1.55",
            },
            H1 = new H1Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "700", FontSize = "2.25rem", LineHeight = "1.15" },
            H2 = new H2Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "700", FontSize = "1.875rem", LineHeight = "1.2" },
            H3 = new H3Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "600", FontSize = "1.5rem", LineHeight = "1.25" },
            H4 = new H4Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "600", FontSize = "1.25rem", LineHeight = "1.35" },
            H5 = new H5Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "600", FontSize = "1.125rem", LineHeight = "1.4" },
            H6 = new H6Typography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "600", FontSize = "1rem", LineHeight = "1.5" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px",
        },
    };
}
