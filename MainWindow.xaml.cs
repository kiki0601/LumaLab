using Microsoft.Win32;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace LumaLab;

public partial class MainWindow : Window
{
    private BitmapSource? original;
    private string? currentPath;
    private bool updating;

    public MainWindow() => InitializeComponent();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|All files|*.*" };
        if (dialog.ShowDialog() != true) return;
        currentPath = dialog.FileName;
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(currentPath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        original = bitmap;
        Preview.Source = original;
        Preview.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
        Status.Text = $"Loaded • {System.IO.Path.GetFileName(currentPath)}";
        Reset_Click(this, new RoutedEventArgs());
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        updating = true;
        Exposure.Value = Contrast.Value = Highlights.Value = Shadows.Value = Whites.Value = Blacks.Value = Temperature.Value = Tint.Value = Saturation.Value = Vibrance.Value = 0;
        updating = false;
        ApplyPreview();
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updating) ApplyPreview();
    }

    private void ApplyPreview()
    {
        if (original is null) return;
        var brightness = Exposure.Value * 0.45;
        var contrast = 1.0 + Contrast.Value / 100.0;
        var saturation = 1.0 + Saturation.Value / 100.0;
        var matrix = new ColorMatrix
        {
            M11 = contrast * saturation,
            M12 = 0,
            M13 = 0,
            M14 = 0,
            M21 = 0,
            M22 = contrast * saturation,
            M23 = 0,
            M24 = 0,
            M31 = 0,
            M32 = 0,
            M33 = contrast * saturation,
            M34 = 0,
            M41 = brightness,
            M42 = brightness,
            M43 = brightness,
            M44 = 1
        };
        Preview.Effect = new ColorMatrixEffect(matrix);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (original is null) return;
        var dialog = new SaveFileDialog { Filter = "JPEG|*.jpg|PNG|*.png", FileName = "LumaLab_Edit.jpg" };
        if (dialog.ShowDialog() != true) return;
        var encoder = dialog.FilterIndex == 2 ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(original));
        using var stream = System.IO.File.Create(dialog.FileName);
        encoder.Save(stream);
        Status.Text = "Exported • " + System.IO.Path.GetFileName(dialog.FileName);
    }
}

public struct ColorMatrix
{
    public double M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44;
}

public sealed class ColorMatrixEffect : Effect
{
    public ColorMatrixEffect(ColorMatrix matrix) { Matrix = matrix; }
    public ColorMatrix Matrix { get; }
}
