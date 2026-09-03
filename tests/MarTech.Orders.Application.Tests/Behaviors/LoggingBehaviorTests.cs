using MarTech.Orders.Application.Authentication.Login;
using MarTech.Orders.Application.Behaviors;
using MarTech.Orders.Application.Orders.CreateOrder;
using Microsoft.Extensions.Logging;

namespace MarTech.Orders.Application.Tests.Behaviors;

public sealed class LoggingBehaviorTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");

    [Fact]
    public async Task Handle_ForSensitiveRequests_KeepsPayloadsOutOfTheLogs()
    {
        var logger = new RecordingLogger<LoggingBehavior<LoginCommand, LoginResponse>>();
        var behavior = new LoggingBehavior<LoginCommand, LoginResponse>(logger);
        var expiresAt = new DateTime(2026, 3, 15, 13, 0, 0, DateTimeKind.Utc);

        await behavior.Handle(
            new LoginCommand("dev@martech.com", "Senha@123"),
            _ => Task.FromResult(new LoginResponse("signed-token", expiresAt)),
            CancellationToken.None);

        logger.Messages.ShouldNotBeEmpty();
        logger.Messages.ShouldAllBe(message => !message.Contains("Senha@123", StringComparison.Ordinal));
        logger.Messages.ShouldAllBe(message => !message.Contains("signed-token", StringComparison.Ordinal));
        logger.Messages.ShouldContain(message => message.Contains("[redacted]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_ForOrdinaryRequests_LogsTheNameAndDuration()
    {
        var logger = new RecordingLogger<LoggingBehavior<CreateOrderCommand, string>>();
        var behavior = new LoggingBehavior<CreateOrderCommand, string>(logger);

        await behavior.Handle(
            new CreateOrderCommand(CustomerId, [new CreateOrderItem("Keyboard", 1, 10m)]),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        logger.Messages.ShouldContain(message => message.Contains(nameof(CreateOrderCommand), StringComparison.Ordinal));
        logger.Messages.ShouldContain(message => message.Contains("ms", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WhenTheHandlerThrows_LogsTheFailureAndRethrows()
    {
        var logger = new RecordingLogger<LoggingBehavior<CreateOrderCommand, string>>();
        var behavior = new LoggingBehavior<CreateOrderCommand, string>(logger);

        await Should.ThrowAsync<InvalidOperationException>(() => behavior.Handle(
            new CreateOrderCommand(CustomerId, [new CreateOrderItem("Keyboard", 1, 10m)]),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None));

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

        public IReadOnlyList<string> Messages => [.. _entries.Select(entry => entry.Message)];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _entries.Add((logLevel, formatter(state, exception)));
    }
}
