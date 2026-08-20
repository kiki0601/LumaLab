namespace LumaLab;

public sealed class BrushMask
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Alpha { get; }

    public BrushMask(int width, int height)
    {
        Width = width;
        Height = height;
        Alpha = new byte[Math.Max(1, width * height)];
    }

    public void Clear() => Array.Clear(Alpha, 0, Alpha.Length);

    public void Invert()
    {
        for (int i = 0; i < Alpha.Length; i++) Alpha[i] = (byte)(255 - Alpha[i]);
    }

    public void Paint(double x, double y, double radius, double flow, double feather, bool erase)
    {
        int left = Math.Max(0, (int)Math.Floor(x - radius));
        int right = Math.Min(Width - 1, (int)Math.Ceiling(x + radius));
        int top = Math.Max(0, (int)Math.Floor(y - radius));
        int bottom = Math.Min(Height - 1, (int)Math.Ceiling(y + radius));
        double safeRadius = Math.Max(1, radius);
        double strength = Math.Clamp(flow, 0, 1);
        double hardEdge = Math.Clamp(1 - feather, 0.001, 1);

        for (int py = top; py <= bottom; py++)
        for (int px = left; px <= right; px++)
        {
            double distance = Math.Sqrt(Math.Pow(px - x, 2) + Math.Pow(py - y, 2)) / safeRadius;
            if (distance > 1) continue;
            double falloff = distance <= hardEdge ? 1 : 1 - (distance - hardEdge) / Math.Max(0.001, 1 - hardEdge);
            falloff = falloff * falloff * (3 - 2 * falloff);
            int index = py * Width + px;
            double current = Alpha[index];
            double target = erase ? 0 : 255;
            Alpha[index] = (byte)Math.Clamp(Math.Round(current + (target - current) * falloff * strength), 0, 255);
        }
    }
}
