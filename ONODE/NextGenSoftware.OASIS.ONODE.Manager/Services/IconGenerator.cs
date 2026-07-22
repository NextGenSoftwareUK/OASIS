using SkiaSharp;

namespace NextGenSoftware.OASIS.ONODE.Manager.Services;

/// <summary>
/// Generates neon ONODE icon PNGs at runtime into ~/.oasis/icons/ if they don't already exist.
/// This avoids needing pre-built asset files in the repo while providing polished icons.
/// </summary>
public static class IconGenerator
{
    private static readonly Dictionary<string, SKColor> IconColors = new()
    {
        ["onode-blue"]   = new SKColor(0,   191, 255),   // #00BFFF — all running
        ["onode-grey"]   = new SKColor(128, 128, 128),   // #808080 — stopped
        ["onode-yellow"] = new SKColor(255, 165, 0),     // #FFA500 — degraded/partial
        ["onode-red"]    = new SKColor(255, 68,  68),    // #FF4444 — error/crashed
    };

    public static string IconDirectory { get; private set; } = "";

    public static void EnsureIconsExist()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".oasis", "icons");
        Directory.CreateDirectory(dir);
        IconDirectory = dir;

        foreach (var (name, color) in IconColors)
        {
            var path = Path.Combine(dir, name + ".png");
            if (!File.Exists(path))
                GeneratePng(path, color, 32);
        }
    }

    public static string GetIconPath(string key)
        => Path.Combine(IconDirectory, $"onode-{key}.png");

    static void GeneratePng(string path, SKColor color, int size)
    {
        using var bmp    = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        float cx = size / 2f;
        float cy = size / 2f;
        float r  = size / 2f - 2;

        // Outer glow
        using var glowPaint = new SKPaint
        {
            Color       = color.WithAlpha(70),
            IsAntialias = true,
            MaskFilter  = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 2.5f)
        };
        canvas.DrawCircle(cx, cy, r, glowPaint);

        // Background fill (dark navy inside)
        using var bgPaint = new SKPaint { Color = new SKColor(10, 10, 30), IsAntialias = true };
        canvas.DrawCircle(cx, cy, r - 1, bgPaint);

        // Coloured ring
        using var ringPaint = new SKPaint
        {
            Color       = color,
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 3.5f
        };
        canvas.DrawCircle(cx, cy, r - 2, ringPaint);

        // "O" letter
        using var textPaint = new SKPaint
        {
            Color       = color,
            IsAntialias = true,
            TextSize    = size * 0.44f,
            FakeBoldText = true,
            TextAlign   = SKTextAlign.Center,
        };
        var metrics = new SKFontMetrics();
        textPaint.GetFontMetrics(out metrics);
        float textY = cy - (metrics.Ascent + metrics.Descent) / 2f;
        canvas.DrawText("O", cx, textY, textPaint);

        using var img  = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
