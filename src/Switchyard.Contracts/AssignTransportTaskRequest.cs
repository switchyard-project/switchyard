using System.ComponentModel.DataAnnotations;

namespace Switchyard.Contracts;

public sealed class AssignTransportTaskRequest
{
    [Required]
    [RegularExpression(@"\A[A-Z0-9][A-Z0-9_-]{0,31}\z")]
    public string RobotId { get; init; } = string.Empty;
}
