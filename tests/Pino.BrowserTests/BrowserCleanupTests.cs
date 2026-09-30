using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class BrowserCleanupTests
{
    [Fact]
    public async Task CaptureFailureDoesNotPreventCleanupAsync()
    {
        var completed = new List<string>();
        await BrowserCleanup.RunAsync(() => throw new IOException("Artifact disk unavailable."),
            () => { completed.Add("browser"); return Task.CompletedTask; },
            () => { completed.Add("database"); return Task.CompletedTask; },
            () => { completed.Add("mail"); return Task.CompletedTask; });
        completed.ShouldBe(["browser", "database", "mail"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CleanupFailureStillRunsEveryLaterStageAsync(int failingStage)
    {
        var completed = new List<int>();
        var failure = new IOException("Injected cleanup failure.");
        var stages = Enumerable.Range(0, 3).Select<int, Func<Task>>(index => () =>
        {
            completed.Add(index);
            return index == failingStage ? Task.FromException(failure) : Task.CompletedTask;
        }).ToArray();
        var exception = await Should.ThrowAsync<AggregateException>(() => BrowserCleanup.RunAsync(() => Task.CompletedTask, stages));
        exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeSameAs(failure);
        completed.ShouldBe([0, 1, 2]);
    }
}
