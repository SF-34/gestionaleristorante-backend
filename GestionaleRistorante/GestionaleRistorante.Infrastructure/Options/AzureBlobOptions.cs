namespace GestionaleRistorante.Infrastructure.Options;

public sealed class AzureBlobOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "dish-images";
}
