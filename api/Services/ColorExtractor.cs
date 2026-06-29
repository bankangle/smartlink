using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SmartLink.Api.Services;

/// <summary>
/// Derives a single vibrant accent color from cover artwork. Powers the
/// page's artwork-driven gradient (the "surprise" theming feature).
/// </summary>
public class ColorExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ColorExtractor> _log;

    public ColorExtractor(IHttpClientFactory http, ILogger<ColorExtractor> log)
    {
        _http = http;
        _log = log;
    }

    /// <summary>Returns a hex color like "#3b82f6", or null if it can't be computed.</summary>
    public async Task<string?> FromImageUrlAsync(string imageUrl, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("artwork");
            await using var stream = await client.GetStreamAsync(imageUrl, ct);
            using var image = await Image.LoadAsync<Rgba32>(stream, ct);

            // Downscale heavily — we only need the broad palette, not detail.
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(48, 48),
                Mode = ResizeMode.Max
            }));

            return PickAccent(image);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to extract accent color from {Url}", imageUrl);
            return null;
        }
    }

    /// <summary>
    /// Picks the most "vibrant" representative color: weight pixels by saturation
    /// and mid-range brightness so we avoid washed-out and near-black/white pixels.
    /// </summary>
    private static string? PickAccent(Image<Rgba32> image)
    {
        double sumR = 0, sumG = 0, sumB = 0, sumW = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (ref var p in row)
                {
                    if (p.A < 128) continue;

                    double r = p.R / 255.0, g = p.G / 255.0, b = p.B / 255.0;
                    double max = Math.Max(r, Math.Max(g, b));
                    double min = Math.Min(r, Math.Min(g, b));
                    double sat = max <= 0 ? 0 : (max - min) / max;
                    double lum = (max + min) / 2.0;

                    // Favor saturated, mid-bright pixels.
                    double brightnessWeight = 1.0 - Math.Abs(lum - 0.5) * 1.6;
                    if (brightnessWeight < 0) brightnessWeight = 0;
                    double w = (sat * sat) * brightnessWeight + 0.02;

                    sumR += r * w;
                    sumG += g * w;
                    sumB += b * w;
                    sumW += w;
                }
            }
        });

        if (sumW <= 0) return null;

        int R = (int)Math.Round(sumR / sumW * 255);
        int G = (int)Math.Round(sumG / sumW * 255);
        int B = (int)Math.Round(sumB / sumW * 255);
        return $"#{R:x2}{G:x2}{B:x2}";
    }
}
