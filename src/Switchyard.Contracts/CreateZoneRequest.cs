using System.ComponentModel.DataAnnotations;

namespace Switchyard.Contracts;

public sealed class CreateZoneRequest
{
    [Required, StringLength(60)]
    public string Name { get; init; } = string.Empty;
    [Required, RegularExpression("\\A(shelf|dock|charging)\\z")]
    public string Kind { get; init; } = string.Empty;
    [Range(1, 50)]
    public int StartRow { get; init; }
    [Range(1, 50)]
    public int StartColumn { get; init; }
    [Range(1, 50)]
    public int Rows { get; init; }
    [Range(1, 50)]
    public int Columns { get; init; }
}
