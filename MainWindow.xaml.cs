using Microsoft.Win32;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace LumaLab;

public partial class MainWindow : Window
{
    private BitmapSource? original;
    private BitmapSource? edited;
    private bool updating;

    public MainWindow() => InitializeComponent();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|All files|*.*" };
        if (dialog.ShowDialog() != true) return;
        var bitmap = new BitmapImage();
        bitmap.BeginInit(); bitmap.UriSource = new Uri(dialog.FileName); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze();
        original = bitmap;
        Preview.Visibility = Visibility.Visible; EmptyText.Visibility = Visibility.Collapsed;
        Status.Text = $"Loaded • {System.IO.Path.GetFileName(dialog.FileName)}";
        Reset_Click(this, new RoutedEventArgs());
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        updating = true;
        Exposure.Value = Contrast.Value = Highlights.Value = Shadows.Value = Whites.Value = Blacks.Value = Temperature.Value = Tint.Value = Saturation.Value = Vibrance.Value = 0;
        updating = false; ApplyPreview();
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updating) ApplyPreview();
    }

    private void ApplyPreview()
    {
        if (original is null) return;
        var source = new FormatConvertedBitmap(original, PixelFormats.Bgra32, null, 0);
        int stride = source.PixelWidth * 4;
        byte[] pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);
        double exposure = Math.Pow(2, Exposure.Value);
        double contrast = 1 + Contrast.Value / 100.0;
        double saturation = 1 + Saturation.Value / 100.0;
        double temp = Temperature.Value / 100.0 * 25;
        double tint = Tint.Value / 100.0 * 18;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            double b = pixels[i] * exposure, g = pixels[i + 1] * exposure, r = pixels[i + 2] * exposure;
            r += temp + tint; b -= temp; g -= tint * 0.35;
            double luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            r = luma + (r - luma) * saturation; g = luma + (g - luma) * saturation; b = luma + (b - luma) * saturation;
            r = 128 + (r - 128) * contrast; g = 128 + (g - 128) * contrast; b = 128 + (b - 128) * contrast;
            pixels[i] = Clamp(b); pixels[i + 1] = Clamp(g); pixels[i + 2] = Clamp(r);
        }
        var result = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
        result.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), pixels, stride, 0);
        result.Freeze(); edited = result; Preview.Source = edited;
    }

    private static byte Clamp(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (edited is null) return;
        var dialog = new SaveFileDialog { Filter = "JPEG|*.jpg|PNG|*.png", FileName = "LumaLab_Edit.jpg" };
        if (dialog.ShowDialog() != true) return;
        BitmapEncoder encoder = dialog.FilterIndex == 2 ? new PngBitmapEncoder() : new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(edited));
        using var stream = System.IO.File.Create(dialog.FileName); encoder.Save(stream);
        Status.Text = "Exported • " + System.IO.Path.GetFileName(dialog.FileName);
    }
}
