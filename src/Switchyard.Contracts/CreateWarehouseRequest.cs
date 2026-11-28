using System.ComponentModel.DataAnnotations;

namespace Switchyard.Contracts;

public sealed class CreateWarehouseRequest
{
    [Required, StringLength(60)]
    public string Name { get; init; } = string.Empty;
    [Range(1, 50)]
    public int Rows { get; init; }
    [Range(1, 50)]
    public int Columns { get; init; }
}
