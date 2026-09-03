using MarTech.Orders.Application.Common.Exceptions;
using MarTech.Orders.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MarTech.Orders.Api.ErrorHandling;

public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            OrderNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Authentication failed"),
            OrderNotCancellableException or OrderNotConfirmableException =>
                (StatusCodes.Status409Conflict, "Invalid order state transition"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }
        });
    }
}
