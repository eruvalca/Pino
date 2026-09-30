using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pino.Data;
using Pino.Features.Clubs.Services;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class PhotoCleanupServiceTests
{
    [Fact]
    public async Task ExhaustedDatabaseRetriesKeepWorkerAliveForNextTickAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var factory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        var failure = new RetryLimitExceededException("Database retries exhausted.", new Npgsql.NpgsqlException("Unavailable."));
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<ApplicationDbContext>(failure));
        var clock = Substitute.For<TimeProvider>();
        var timer = Substitute.For<ITimer>();
        var created = new TaskCompletionSource<(TimerCallback Callback, object? State)>(TaskCreationOptions.RunContinuationsAsynchronously);
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
            .Returns(call => { created.SetResult((call.ArgAt<TimerCallback>(0), call.ArgAt<object?>(1))); return timer; });
        var failures = Channel.CreateUnbounded<Exception>();
        using var service = new PhotoCleanupService(factory, Substitute.For<IProfilePhotoStore>(), clock, new FailureLogger(failures.Writer));
        await service.StartAsync(cancellationToken);
        try
        {
            // Test watchdogs use real time; the substitute clock only drives the worker's timer.
            var tick = await created.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, cancellationToken);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                tick.Callback(tick.State);
                var logged = await failures.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, cancellationToken);
                logged.ShouldBeSameAs(failure);
                service.ExecuteTask!.IsCompleted.ShouldBeFalse();
            }
            await factory.Received(2).CreateDbContextAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            await service.StopAsync(cancellationToken);
        }
        service.ExecuteTask!.IsFaulted.ShouldBeFalse();
        timer.Received(1).Dispose();
    }

    private sealed class FailureLogger(ChannelWriter<Exception> failures) : ILogger<PhotoCleanupService>
    {
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
            {
                failures.TryWrite(exception);
            }
        }
    }
}
