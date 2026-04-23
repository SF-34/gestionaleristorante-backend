namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
