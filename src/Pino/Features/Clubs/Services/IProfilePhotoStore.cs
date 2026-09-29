namespace Pino.Features.Clubs.Services;

internal interface IProfilePhotoStore
{
    Task UploadAsync(string key, byte[] jpeg, CancellationToken cancellationToken);
    Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
