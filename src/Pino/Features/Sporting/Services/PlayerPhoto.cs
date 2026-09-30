using SkiaSharp;

namespace Pino.Features.Sporting.Services;

internal static class PlayerPhoto
{
    internal static byte[] Normalize(string encoded)
    {
        if (encoded.Length > 7000000) { throw new InvalidDataException("Choose a photo no larger than 5 MB."); }
        using var data = new MemoryStream(Convert.FromBase64String(encoded), writable: false);
        using var codec = SKCodec.Create(data, out var status);
        if (status != SKCodecResult.Success || codec.Info.Width is < 1 or > 4096 || codec.Info.Height is < 1 or > 4096 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
        {
            throw new InvalidDataException("Choose a JPEG or PNG up to 4096 pixels on each side.");
        }
        using var bitmap = new SKBitmap(codec.Info);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) { throw new InvalidDataException("The photo is incomplete."); }
        var swapsAxes = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        using var oriented = new SKBitmap(swapsAxes ? bitmap.Height : bitmap.Width, swapsAxes ? bitmap.Width : bitmap.Height);
        Orient(bitmap, oriented, codec.EncodedOrigin);
        var side = Math.Min(oriented.Width, oriented.Height);
        var source = SKRect.Create((oriented.Width - side) / 2f, (oriented.Height - side) / 2f, side, side);
        using var surface = SKSurface.Create(new SKImageInfo(512, 512));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawBitmap(oriented, source, SKRect.Create(512, 512), SKSamplingOptions.Default);
        using var image = surface.Snapshot();
        using var output = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return output.ToArray();
    }

    private static void Orient(SKBitmap bitmap, SKBitmap result, SKEncodedOrigin origin)
    {
        using var canvas = new SKCanvas(result);
        // These transforms map the encoded pixel edges into upright image coordinates.
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, bitmap.Width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, bitmap.Width, 0, -1, bitmap.Height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, bitmap.Height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, bitmap.Height, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, bitmap.Height, -1, 0, bitmap.Width, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, bitmap.Width, 0, 0, 1),
            _ => SKMatrix.Identity,
        };
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);
    }
}
