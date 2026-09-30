using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Services;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class PlayerPhotoTests
{
    [Fact]
    public void PhotoIsReencodedAsBoundedSquareJpeg()
    {
        using var bitmap = new SKBitmap(96, 64);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var original = SKImage.FromBitmap(bitmap);
        using var encoded = original.Encode(SKEncodedImageFormat.Png, 100);
        var result = PlayerPhoto.Normalize(Convert.ToBase64String(encoded.ToArray()));
        using var stream = new MemoryStream(result);
        using var codec = SKCodec.Create(stream);
        codec.Info.Width.ShouldBe(512);
        codec.Info.Height.ShouldBe(512);
        codec.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
        result.Length.ShouldBeLessThan(100000);
    }

    [Fact]
    public void InvalidOrOversizedPhotoIsRejected()
    {
        Should.Throw<FormatException>(() => PlayerPhoto.Normalize("not base64!"));
        Should.Throw<InvalidDataException>(() => PlayerPhoto.Normalize(Convert.ToBase64String("not an image"u8.ToArray())));
        Should.Throw<InvalidDataException>(() => PlayerPhoto.Normalize(new('A', 7000001)));
    }

    [Theory]
    [InlineData(1, "RGBY")]
    [InlineData(2, "GRYB")]
    [InlineData(3, "YBGR")]
    [InlineData(4, "BYRG")]
    [InlineData(5, "RBGY")]
    [InlineData(6, "BRYG")]
    [InlineData(7, "YGBR")]
    [InlineData(8, "GYRB")]
    public void ExifOrientationIsAppliedBeforeMetadataIsRemoved(int orientation, string corners)
    {
        ArgumentNullException.ThrowIfNull(corners);
        using var bitmap = new SKBitmap(96, 64);
        SKColor[] colors = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow];
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                bitmap.SetPixel(x, y, colors[(y < 32 ? 0 : 2) + (x < 48 ? 0 : 1)]);
            }
        }
        using var original = SKImage.FromBitmap(bitmap);
        using var encoded = original.Encode(SKEncodedImageFormat.Jpeg, 100);
        var jpeg = encoded.ToArray();
        // EXIF APP1 segment: little-endian TIFF with one SHORT orientation tag (0x0112).
        byte[] exif = [255, 225, 0, 34, 69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0,
            1, 0, 18, 1, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        byte[] orientedJpeg = [.. jpeg.AsSpan(0, 2), .. exif, .. jpeg.AsSpan(2)];
        using var sourceStream = new MemoryStream(orientedJpeg);
        using var sourceCodec = SKCodec.Create(sourceStream);
        sourceCodec.EncodedOrigin.ShouldBe((SKEncodedOrigin)orientation);

        var result = PlayerPhoto.Normalize(Convert.ToBase64String(orientedJpeg));
        using var normalized = SKBitmap.Decode(result);
        normalized.Width.ShouldBe(512);
        normalized.Height.ShouldBe(512);
        for (var corner = 0; corner < 4; corner++)
        {
            var actual = normalized.GetPixel(corner % 2 == 0 ? 128 : 384, corner < 2 ? 128 : 384);
            var expected = corners[corner] switch { 'R' => SKColors.Red, 'G' => SKColors.Lime, 'B' => SKColors.Blue, _ => SKColors.Yellow };
            Math.Abs(actual.Red - expected.Red).ShouldBeLessThan(20);
            Math.Abs(actual.Green - expected.Green).ShouldBeLessThan(20);
            Math.Abs(actual.Blue - expected.Blue).ShouldBeLessThan(20);
        }
        using var resultStream = new MemoryStream(result);
        using var resultCodec = SKCodec.Create(resultStream);
        resultCodec.EncodedOrigin.ShouldBe(SKEncodedOrigin.TopLeft);
    }
}
