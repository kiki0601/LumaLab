using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LumaLab;

public partial class MainWindow : Window
{
    private BitmapSource? original;
    private BitmapSource? edited;
    private BrushMask? mask;
    private bool updating;
    private bool painting;
    private bool eraseMode;
    private bool overlayVisible = true;

    public MainWindow() => InitializeComponent();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|All files|*.*" };
        if (dialog.ShowDialog() != true) return;
        var bitmap = new BitmapImage();
        bitmap.BeginInit(); bitmap.UriSource = new Uri(dialog.FileName); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze();
        original = bitmap;
        mask = new BrushMask(bitmap.PixelWidth, bitmap.PixelHeight);
        Preview.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
        PaintSurface.Visibility = Visibility.Collapsed;
        BrushCursor.Visibility = Visibility.Collapsed;
        Status.Text = $"Loaded • {System.IO.Path.GetFileName(dialog.FileName)}";
        Reset_Click(this, new RoutedEventArgs());
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        updating = true;
        Exposure.Value = Contrast.Value = Highlights.Value = Shadows.Value = Whites.Value = Blacks.Value = Temperature.Value = Tint.Value = Saturation.Value = Vibrance.Value = 0;
        MaskExposure.Value = MaskContrast.Value = MaskTemperature.Value = MaskTint.Value = MaskSaturation.Value = 0;
        BrushSize.Value = 80; BrushFeather.Value = 75; BrushFlow.Value = 100;
        updating = false;
        mask?.Clear();
        overlayVisible = true;
        MaskOverlay.Visibility = Visibility.Collapsed;
        ApplyPreview();
        Status.Text = original is null ? "Ready" : "Reset • Mask cleared";
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updating) ApplyPreview();
    }

    private void MaskSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updating) ApplyPreview();
    }

    private void BrushTool_Click(object sender, RoutedEventArgs e)
    {
        if (original is null) return;
        PaintSurface.Visibility = Visibility.Visible;
        BrushCursor.Visibility = Visibility.Visible;
        Status.Text = eraseMode ? "Brush Mask • Erase" : "Brush Mask • Paint";
        UpdateOverlay();
    }

    private void ToggleErase_Click(object sender, RoutedEventArgs e)
    {
        eraseMode = !eraseMode;
        EraseButton.Content = eraseMode ? "PAINT" : "ERASE";
        Status.Text = eraseMode ? "Brush Mask • Erase" : "Brush Mask • Paint";
    }

    private void ShowMask_Click(object sender, RoutedEventArgs e)
    {
        overlayVisible = !overlayVisible;
        MaskOverlay.Visibility = overlayVisible && mask is not null ? Visibility.Visible : Visibility.Collapsed;
        Status.Text = overlayVisible ? "Mask overlay • ON" : "Mask overlay • OFF";
    }

    private void ClearMask_Click(object sender, RoutedEventArgs e)
    {
        mask?.Clear();
        UpdateOverlay();
        ApplyPreview();
        Status.Text = "Mask cleared";
    }

    private void InvertMask_Click(object sender, RoutedEventArgs e)
    {
        mask?.Invert();
        UpdateOverlay();
        ApplyPreview();
        Status.Text = "Mask inverted";
    }

    private void BrushSetting_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrushCursor.Visibility == Visibility.Visible) UpdateBrushCursorSize();
    }

    private void PaintSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (mask is null) return;
        painting = true;
        PaintSurface.CaptureMouse();
        PaintAt(e.GetPosition(PaintSurface));
    }

    private void PaintSurface_MouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(PaintSurface);
        UpdateBrushCursor(point);
        if (painting && e.LeftButton == MouseButtonState.Pressed) PaintAt(point);
    }

    private void PaintSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        painting = false;
        PaintSurface.ReleaseMouseCapture();
        ApplyPreview();
    }

    private void PaintSurface_MouseLeave(object sender, MouseEventArgs e)
    {
        BrushCursor.Visibility = painting ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PaintAt(Point hostPoint)
    {
        if (mask is null || original is null) return;
        if (!TryHostToImage(hostPoint, out double ix, out double iy, out double scale)) return;
        double radius = Math.Max(1, BrushSize.Value / scale / 2.0);
        double feather = BrushFeather.Value / 100.0;
        double flow = BrushFlow.Value / 100.0;
        mask.Paint(ix, iy, radius, flow, feather, eraseMode);
        UpdateOverlay();
        ApplyPreview();
    }

    private void UpdateBrushCursorSize()
    {
        double size = Math.Max(5, BrushSize.Value);
        BrushCursor.Width = size;
        BrushCursor.Height = size;
    }

    private void UpdateBrushCursor(Point point)
    {
        UpdateBrushCursorSize();
        Canvas.SetLeft(BrushCursor, point.X - BrushCursor.Width / 2);
        Canvas.SetTop(BrushCursor, point.Y - BrushCursor.Height / 2);
        BrushCursor.Visibility = PaintSurface.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool TryHostToImage(Point host, out double imageX, out double imageY, out double scale)
    {
        imageX = imageY = scale = 0;
        if (original is null || CanvasHost.ActualWidth <= 0 || CanvasHost.ActualHeight <= 0) return false;
        double sx = CanvasHost.ActualWidth / original.PixelWidth;
        double sy = CanvasHost.ActualHeight / original.PixelHeight;
        scale = Math.Min(sx, sy);
        double displayedW = original.PixelWidth * scale;
        double displayedH = original.PixelHeight * scale;
        double left = (CanvasHost.ActualWidth - displayedW) / 2.0;
        double top = (CanvasHost.ActualHeight - displayedH) / 2.0;
        imageX = (host.X - left) / scale;
        imageY = (host.Y - top) / scale;
        return imageX >= 0 && imageY >= 0 && imageX < original.PixelWidth && imageY < original.PixelHeight;
    }

    private void UpdateOverlay()
    {
        if (mask is null) return;
        var pixels = new byte[mask.Width * mask.Height * 4];
        for (int i = 0; i < mask.Alpha.Length; i++)
        {
            byte a = mask.Alpha[i];
            int p = i * 4;
            pixels[p] = a;
            pixels[p + 1] = 40;
            pixels[p + 2] = 255;
            pixels[p + 3] = a;
        }
        var bitmap = new WriteableBitmap(mask.Width, mask.Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, mask.Width, mask.Height), pixels, mask.Width * 4, 0);
        bitmap.Freeze();
        MaskOverlay.Source = bitmap;
        MaskOverlay.Visibility = overlayVisible ? Visibility.Visible : Visibility.Collapsed;
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

            if (mask is not null && i / 4 < mask.Alpha.Length)
            {
                double m = mask.Alpha[i / 4] / 255.0;
                if (m > 0)
                {
                    double me = Math.Pow(2, MaskExposure.Value);
                    double mc = 1 + MaskContrast.Value / 100.0;
                    double mt = MaskTemperature.Value / 100.0 * 25;
                    double mi = MaskTint.Value / 100.0 * 18;
                    double ms = 1 + MaskSaturation.Value / 100.0;
                    double mr = r * me, mg = g * me, mb = b * me;
                    mr += mt + mi; mb -= mt; mg -= mi * 0.35;
                    double ml = 0.2126 * mr + 0.7152 * mg + 0.0722 * mb;
                    mr = ml + (mr - ml) * ms; mg = ml + (mg - ml) * ms; mb = ml + (mb - ml) * ms;
                    mr = 128 + (mr - 128) * mc; mg = 128 + (mg - 128) * mc; mb = 128 + (mb - 128) * mc;
                    r += (mr - r) * m; g += (mg - g) * m; b += (mb - b) * m;
                }
            }

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
