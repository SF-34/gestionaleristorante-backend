using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public class CreateTableRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Capacity { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "available";
}

public sealed class UpdateTableRequest : CreateTableRequest;

public sealed class TableResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = string.Empty;
}
