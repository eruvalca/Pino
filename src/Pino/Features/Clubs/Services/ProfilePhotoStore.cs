using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Pino.Features.Clubs.Services;

internal sealed class ProfilePhotoStore(BlobServiceClient blobs) : IProfilePhotoStore
{
    private BlobContainerClient Container => blobs.GetBlobContainerClient("profile-photos");

    public async Task UploadAsync(string key, byte[] jpeg, CancellationToken cancellationToken)
    {
        await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await using var stream = new MemoryStream(jpeg, writable: false);
        await Container.GetBlobClient(key).UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg", CacheControl = "no-store" },
        }, cancellationToken);
    }

    public async Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var result = await Container.GetBlobClient(key).DownloadContentAsync(cancellationToken);
        return result.Value.Content.ToArray();
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        await Container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
