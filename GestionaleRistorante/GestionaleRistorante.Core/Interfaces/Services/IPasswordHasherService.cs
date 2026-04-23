namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IPasswordHasherService
{
    string HashPassword(string password);

    bool VerifyPassword(string hash, string password);
}
