using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class BlobStorageService(IOptions<AzureBlobOptions> blobOptions) : IBlobStorageService
{
    private readonly AzureBlobOptions _blobOptions = blobOptions.Value;

    public async Task<string> UploadDishImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_blobOptions.ConnectionString))
        {
            throw new AppException("BLOB_NOT_CONFIGURED", "Azure Blob Storage is not configured.", 500);
        }

        var blobServiceClient = new BlobServiceClient(_blobOptions.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_blobOptions.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var safeFileName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var blobClient = containerClient.GetBlobClient(safeFileName);

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);

        return blobClient.Uri.ToString();
    }
}
