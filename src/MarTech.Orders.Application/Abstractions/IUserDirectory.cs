using MarTech.Orders.Application.Authentication;

namespace MarTech.Orders.Application.Abstractions;

public interface IUserDirectory
{
    AuthenticatedUser? Authenticate(string email, string password);
}
