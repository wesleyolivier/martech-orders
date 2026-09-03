using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common.Exceptions;
using MediatR;

namespace MarTech.Orders.Application.Authentication.Login;

public sealed class LoginCommandHandler(IUserDirectory users, IAccessTokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = users.Authenticate(request.Email, request.Password) ?? throw new InvalidCredentialsException();

        var token = tokenIssuer.Issue(user);

        return Task.FromResult(new LoginResponse(token.Value, token.ExpiresAtUtc));
    }
}
