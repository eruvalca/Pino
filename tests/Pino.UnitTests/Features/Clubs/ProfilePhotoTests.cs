using System.Diagnostics.CodeAnalysis;
using System.Text;
using Pino.Features.Clubs.Services;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Pino.UnitTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class ProfilePhotoTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    public void SavedCropIsJpegWithoutOriginalMetadata(SKEncodedImageFormat format)
    {
        var source = CreateImage(512, 512, format);
        if (format == SKEncodedImageFormat.Jpeg)
        {
            var comment = Encoding.ASCII.GetBytes("Private metadata");
            source = [0xff, 0xd8, 0xff, 0xfe, 0, (byte)(comment.Length + 2), .. comment, .. source.AsSpan(2)];
        }
        var bytes = ProfilePhoto.Normalize(Convert.ToBase64String(source));
        using var data = SKData.CreateCopy(bytes);
        using var saved = SKCodec.Create(data);
        saved.Info.Width.ShouldBe(512);
        saved.Info.Height.ShouldBe(512);
        saved.EncodedFormat.ShouldBe(SKEncodedImageFormat.Jpeg);
        Encoding.ASCII.GetString(bytes).ShouldNotContain("Private metadata");
    }

    [Theory]
    [InlineData(511, 512)]
    [InlineData(512, 513)]
    [InlineData(1024, 1024)]
    public void RejectsUncroppedDimensions(int width, int height) =>
        Should.Throw<InvalidDataException>(() => ProfilePhoto.Normalize(Convert.ToBase64String(CreateImage(width, height, SKEncodedImageFormat.Png))));

    [Fact]
    public void RejectsInvalidAndOversizedPayloads()
    {
        Should.Throw<FormatException>(() => ProfilePhoto.Normalize("not-base64"));
        Should.Throw<InvalidDataException>(() => ProfilePhoto.Normalize(new('A', ProfilePhoto.MaximumEncodedLength + 1)));
        Should.Throw<InvalidDataException>(() => ProfilePhoto.Normalize(Convert.ToBase64String([1, 2, 3, 4])));
    }

    [Fact]
    public void RejectsUnsupportedFormatEvenAtCorrectDimensions() =>
        Should.Throw<InvalidDataException>(() => ProfilePhoto.Normalize(Convert.ToBase64String(CreateImage(512, 512, SKEncodedImageFormat.Webp))));

    private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(SKColors.CornflowerBlue);
        using var image = surface.Snapshot();
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }
}
