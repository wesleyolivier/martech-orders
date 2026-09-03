using System.ComponentModel.DataAnnotations;

namespace MarTech.Orders.Infrastructure.Authentication;

public sealed class SeedUserOptions
{
    public const string SectionName = "Authentication:SeedUser";

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string DisplayName { get; init; } = string.Empty;
}
