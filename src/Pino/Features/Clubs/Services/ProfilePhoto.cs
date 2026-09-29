using SkiaSharp;

namespace Pino.Features.Clubs.Services;

internal static class ProfilePhoto
{
    internal const int MaximumEncodedLength = 1400000;

    internal static byte[] Normalize(string encoded)
    {
        if (encoded.Length > MaximumEncodedLength)
        {
            throw new InvalidDataException("The cropped photo is too large.");
        }
        using var data = new MemoryStream(Convert.FromBase64String(encoded), writable: false);
        using var codec = SKCodec.Create(data, out var decodeStatus);
        // Inspect dimensions before allocating decoded pixels. Decode only one fixed-size frame.
        if (decodeStatus != SKCodecResult.Success || codec.Info.Width != 512 || codec.Info.Height != 512 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
        {
            throw new InvalidDataException("Use the crop controls to save a square profile photo.");
        }
        using var bitmap = new SKBitmap(new SKImageInfo(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            throw new InvalidDataException("The cropped photo is incomplete or invalid.");
        }
        using var surface = SKSurface.Create(new SKImageInfo(512, 512));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);
        // Encode only the decoded pixels; original metadata and appended data are discarded.
        using var image = surface.Snapshot();
        using var output = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return output.ToArray();
    }
}
