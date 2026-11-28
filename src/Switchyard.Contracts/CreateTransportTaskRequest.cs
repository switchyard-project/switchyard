using System.ComponentModel.DataAnnotations;

namespace Switchyard.Contracts;

public sealed class CreateTransportTaskRequest
{
    [Required]
    [StringLength(60)]
    public string Pickup { get; init; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Destination { get; init; } = string.Empty;
    [Range(1, long.MaxValue)]
    public long WarehouseId { get; init; } = 1;
}
