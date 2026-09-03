using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Authentication;
using MarTech.Orders.Application.Authentication.Login;
using MarTech.Orders.Application.Common.Exceptions;
using NSubstitute;

namespace MarTech.Orders.Application.Tests.Authentication;

public sealed class LoginCommandHandlerTests
{
    private static readonly DateTime ExpiresAt = new(2026, 3, 15, 13, 0, 0, DateTimeKind.Utc);

    private readonly IUserDirectory _users = Substitute.For<IUserDirectory>();
    private readonly IAccessTokenIssuer _tokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests() => _handler = new LoginCommandHandler(_users, _tokenIssuer);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsBearerToken()
    {
        var user = new AuthenticatedUser(Guid.CreateVersion7(), "dev@martech.com", "MarTech Developer");
        _users.Authenticate("dev@martech.com", "Senha@123").Returns(user);
        _tokenIssuer.Issue(user).Returns(new AccessToken("signed-token", ExpiresAt));

        var response = await _handler.Handle(
            new LoginCommand("dev@martech.com", "Senha@123"),
            CancellationToken.None);

        response.AccessToken.ShouldBe("signed-token");
        response.ExpiresAtUtc.ShouldBe(ExpiresAt);
        response.TokenType.ShouldBe("Bearer");
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_ThrowsAndIssuesNoToken()
    {
        _users.Authenticate(Arg.Any<string>(), Arg.Any<string>()).Returns((AuthenticatedUser?)null);

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _handler.Handle(new LoginCommand("dev@martech.com", "wrong"), CancellationToken.None));

        _tokenIssuer.DidNotReceive().Issue(Arg.Any<AuthenticatedUser>());
    }
}
