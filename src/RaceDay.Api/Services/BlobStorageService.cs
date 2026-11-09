using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

public class AzureBlobOptions
{
    public string? ConnectionString { get; set; }
    public string ContainerName { get; set; } = "raceday-images";
}

public class BlobStorageNotConfiguredException : Exception
{
    public BlobStorageNotConfiguredException()
        : base("Azure Blob Storage is not configured (AzureBlobStorage:ConnectionString is empty).")
    {
    }
}

public interface IBlobStorageService
{
    /// <summary>Uploads an image and returns a URL the browser can load it from.</summary>
    Task<string> UploadImageAsync(Stream content, string blobPath, string contentType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores uploaded images in an Azure Blob Storage container.
/// If the storage account allows public blob access, images get a plain public URL.
/// If it does not (the default on many new accounts), the container stays private and each image gets a
/// long-lived read-only SAS link instead, so uploads work without changing any Azure setting.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly AzureBlobOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BlobContainerClient? _container;
    private bool _publicRead;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadImageAsync(Stream content, string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        var blob = container.GetBlobClient(blobPath);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);

        if (_publicRead || !blob.CanGenerateSasUri)
        {
            return blob.Uri.ToString();
        }

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddYears(5)).ToString();
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (_container is not null)
        {
            return _container;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new BlobStorageNotConfiguredException();
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);

                try
                {
                    await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
                {
                    // The account forbids public access: use a private container and SAS links instead.
                    await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                }

                var properties = await container.GetPropertiesAsync(cancellationToken: cancellationToken);
                _publicRead = properties.Value.PublicAccess is PublicAccessType.Blob or PublicAccessType.BlobContainer;
                _container = container;
            }
        }
        finally
        {
            _lock.Release();
        }

        return _container;
    }
}
