namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IBlobStorageService
{
    Task<string> UploadDishImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
