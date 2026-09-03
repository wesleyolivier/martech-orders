using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Authentication;
using Microsoft.Extensions.Options;

namespace MarTech.Orders.Infrastructure.Authentication;

public sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly AuthenticatedUser _user;
    private readonly byte[] _salt;
    private readonly byte[] _passwordHash;

    public InMemoryUserDirectory(IOptions<SeedUserOptions> options)
    {
        var seed = options.Value;

        _user = new AuthenticatedUser(Guid.CreateVersion7(), seed.Email, seed.DisplayName);
        _salt = PasswordHasher.CreateSalt();
        _passwordHash = PasswordHasher.Hash(seed.Password, _salt);
    }

    public AuthenticatedUser? Authenticate(string email, string password)
    {
        var emailMatches = string.Equals(email, _user.Email, StringComparison.OrdinalIgnoreCase);
        var passwordMatches = PasswordHasher.Verify(password, _salt, _passwordHash);

        return emailMatches && passwordMatches ? _user : null;
    }
}
