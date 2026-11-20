using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MangaVault.Wpf.Services;

namespace MangaVault.Wpf.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
            flag = !flag;

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasText = value is string text && !string.IsNullOrWhiteSpace(text);
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
            hasText = !hasText;

        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value is not null;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
            isNotNull = !isNotNull;

        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class CoverPathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            string trimmed = path.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
            {
                string fullPath = Path.GetFullPath(trimmed);
                if (!File.Exists(fullPath))
                    return null;

                uri = new Uri(fullPath, UriKind.Absolute);
            }
            else if (uri.IsFile && !File.Exists(uri.LocalPath))
            {
                return null;
            }
            else if (!uri.IsFile)
            {
                // Solo portadas locales en modo offline.
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.DecodePixelWidth = 280;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or UriFormatException or ArgumentException)
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class AccentKeyToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string resourceKey = ResolveBrushKey(value, subtle: false);
        return System.Windows.Application.Current?.TryFindResource(resourceKey) as Brush ?? Brushes.MediumPurple;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    internal static string ResolveBrushKey(object? value, bool subtle)
    {
        string prefix = value switch
        {
            NotificationKind.Success => "Success",
            NotificationKind.Info => "Secondary",
            NotificationKind.Warning => "Warning",
            NotificationKind.Error => "Error",
            string key => key switch
            {
                "Secondary" => "Secondary",
                "Success" => "Success",
                "Warning" => "Warning",
                "Error" => "Error",
                _ => "Primary"
            },
            _ => "Primary"
        };

        if (subtle)
            return $"{prefix}SubtleBrush";

        return prefix switch
        {
            "Primary" => "PrimaryAccentBrush",
            "Secondary" => "SecondaryAccentBrush",
            _ => $"{prefix}Brush"
        };
    }
}

public sealed class AccentKeyToSubtleBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string resourceKey = AccentKeyToBrushConverter.ResolveBrushKey(value, subtle: true);
        return System.Windows.Application.Current?.TryFindResource(resourceKey) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class RatioToWidthMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not double ratio
            || values[1] is not double containerWidth
            || double.IsNaN(ratio)
            || double.IsNaN(containerWidth)
            || double.IsInfinity(containerWidth)
            || containerWidth <= 0)
        {
            return 0.0;
        }

        double clamped = Math.Clamp(ratio, 0.0, 1.0);
        return clamped * containerWidth;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        [Binding.DoNothing, Binding.DoNothing];
}
