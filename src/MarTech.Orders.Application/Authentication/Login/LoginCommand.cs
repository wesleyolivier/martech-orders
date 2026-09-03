using MarTech.Orders.Application.Abstractions;
using MediatR;

namespace MarTech.Orders.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>, ISensitiveRequest;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
