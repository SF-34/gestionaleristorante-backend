namespace GestionaleRistorante.Core.Enums;

public static class OrderStatuses
{
    public const string Pending = "pending";

    public const string InPreparation = "in_preparation";

    public const string Ready = "ready";

    public const string Served = "served";

    public const string Cancelled = "cancelled";

    public static readonly ISet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        InPreparation,
        Ready,
        Served,
        Cancelled
    };

    public static readonly ISet<string> FinalStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Served,
        Cancelled
    };
}
