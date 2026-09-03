using MarTech.Orders.Application.Authentication;

namespace MarTech.Orders.Application.Abstractions;

public interface IAccessTokenIssuer
{
    AccessToken Issue(AuthenticatedUser user);
}
