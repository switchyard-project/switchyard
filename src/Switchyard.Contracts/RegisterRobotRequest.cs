using System.ComponentModel.DataAnnotations;

namespace Switchyard.Contracts;

public sealed class RegisterRobotRequest
{
    [Required(ErrorMessage = "El identificador es obligatorio.")]
    [RegularExpression(@"\A[A-Z0-9][A-Z0-9_-]{0,31}\z",
        ErrorMessage = "Usa entre 1 y 32 caracteres: mayúsculas, números, guiones o guiones bajos. Empieza por una letra o un número.")]
    public string Id { get; init; } = string.Empty;
}
