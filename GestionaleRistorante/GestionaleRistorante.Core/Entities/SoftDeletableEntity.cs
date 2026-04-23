namespace GestionaleRistorante.Core.Entities;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
}
