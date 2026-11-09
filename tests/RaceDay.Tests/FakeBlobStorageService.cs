using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Stand-in for Azure Blob Storage so upload endpoints can be tested without a storage account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(Stream content, string blobPath, string contentType, CancellationToken cancellationToken = default)
        => Task.FromResult($"https://fake.blob.core.windows.net/raceday-images/{blobPath}");
}
