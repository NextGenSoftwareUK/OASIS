using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Application.Extensions;

/// <summary>
/// Provides functionality to optimize images by converting them into the WebP format.
/// This class utilizes ImageSharp library to handle image manipulation and WebP encoding.
/// </summary>
public static class ImageOptimizer
{
    /// <summary>
    /// Optimizes the given image data by converting it to the WebP format with reduced quality.
    /// This reduces the image size while attempting to preserve quality.
    /// </summary>
    /// <param name="imageData">The raw byte array representing the image to be optimized.</param>
    /// <param name="quality"></param>
    /// <returns>
    /// A byte array containing the optimized image in WebP format.
    /// </returns>
    public static async Task<byte[]> OptimizeImageAsync(byte[] imageData, int quality)
    {
        using Image image = Image.Load(imageData);

        WebpEncoder webpOptions = new()
        {
            Quality = quality
        };

        using MemoryStream ms = new();

        await image.SaveAsync(ms, webpOptions);

        return ms.ToArray();
    }
}