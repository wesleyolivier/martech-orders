namespace MarTech.Orders.Application.Authentication;

public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName);
