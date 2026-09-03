using System.Diagnostics;
using MarTech.Orders.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MarTech.Orders.Application.Behaviors;

public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;
    private const string Redacted = "[redacted]";

    private static readonly bool CarriesSecrets = typeof(ISensitiveRequest).IsAssignableFrom(typeof(TRequest));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var timestamp = Stopwatch.GetTimestamp();
        object? requestPayload = CarriesSecrets ? Redacted : request;

        LogStarted(logger, requestName, requestPayload);

        try
        {
            var response = await next(cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            object? responsePayload = CarriesSecrets ? Redacted : response;

            if (elapsed >= SlowRequestThresholdMs)
            {
                LogCompletedSlowly(logger, requestName, elapsed, responsePayload);
            }
            else
            {
                LogCompleted(logger, requestName, elapsed, responsePayload);
            }

            return response;
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, requestName, Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
            throw;
        }
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Handling {RequestName} {@Request}")]
    private static partial void LogStarted(ILogger logger, string requestName, object? request);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Handled {RequestName} in {ElapsedMilliseconds:0.0000} ms {@Response}")]
    private static partial void LogCompleted(
        ILogger logger,
        string requestName,
        double elapsedMilliseconds,
        object? response);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Handled {RequestName} in {ElapsedMilliseconds:0.0000} ms which exceeds the slow request threshold {@Response}")]
    private static partial void LogCompletedSlowly(
        ILogger logger,
        string requestName,
        double elapsedMilliseconds,
        object? response);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "{RequestName} failed after {ElapsedMilliseconds:0.0000} ms")]
    private static partial void LogFailed(
        ILogger logger,
        Exception exception,
        string requestName,
        double elapsedMilliseconds);
}
