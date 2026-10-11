using Ocyfrovka.Queue;

namespace Ocyfrovka.Core.Tests;

public sealed class ProcessingQueueTests
{
    [Fact]
    public async Task RunAsync_ProcessesInFifoOrder()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("A", "First");
        queue.Enqueue("B", "Second");
        queue.Enqueue("C", "Third");
        var seen = new List<string>();

        var result = await queue.RunAsync((value, _) =>
        {
            seen.Add(value);
            return Task.CompletedTask;
        });

        Assert.Equal(["A", "B", "C"], seen);
        Assert.Equal(3, result.Completed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Pending);
        Assert.Equal(100, result.Percent);
        Assert.All(queue.Snapshot(), job => Assert.Equal(QueueJobStatus.Completed, job.Status));
    }

    [Fact]
    public async Task RunAsync_FailureIsIsolatedAndNextJobContinues()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("bad", "Bad");
        queue.Enqueue("good", "Good");

        var seen = new List<string>();
        var report = await queue.RunAsync((value, _) =>
        {
            seen.Add(value);
            if (value == "bad")
                throw new InvalidOperationException("Synthetic failure");
            return Task.CompletedTask;
        });

        Assert.Equal(["bad", "good"], seen);
        Assert.Equal(1, report.Failed);
        Assert.Equal(1, report.Completed);
        Assert.Equal(QueueJobStatus.Failed, queue.Snapshot()[0].Status);
        Assert.Contains("Synthetic failure", queue.Snapshot()[0].Error);
    }

    [Fact]
    public async Task RunAsync_BoundedAttemptsRetryWithoutSkippingItem()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("A", "Retryable");
        queue.Enqueue("B", "After");
        var seen = new List<string>();
        var failures = 0;

        await queue.RunAsync((value, _) =>
        {
            seen.Add(value);
            if (value == "A" && failures++ < 2)
                throw new InvalidOperationException("Temporary");
            return Task.CompletedTask;
        }, maxAttempts: 3);

        Assert.Equal(["A", "A", "A", "B"], seen);
        Assert.Equal(3, queue.Snapshot().Single(x => x.DisplayName == "Retryable").Attempts);
        Assert.Equal(2, queue.GetProgress().Completed);
    }

    [Fact]
    public async Task RetryFailed_OnlyRequeuesFailedJobs()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("A", "Bad");
        queue.Enqueue("B", "Good");

        await queue.RunAsync((value, _) =>
        {
            if (value == "A") throw new InvalidOperationException("Failed");
            return Task.CompletedTask;
        });

        Assert.Equal(1, queue.RetryFailed());
        var processed = new List<string>();
        var summary = await queue.RunAsync((value, _) =>
        {
            processed.Add(value);
            return Task.CompletedTask;
        });

        Assert.Equal(["A"], processed);
        Assert.Equal(2, summary.Completed);
        Assert.Equal(0, summary.Failed);
    }

    [Fact]
    public async Task Cancellation_MarksActiveAndPendingItemsCanceled()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("A", "Active");
        queue.Enqueue("B", "Waiting");
        using var cts = new CancellationTokenSource();

        var summary = await queue.RunAsync((_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, cancellationToken: cts.Token);

        Assert.Equal(2, summary.Canceled);
        Assert.Equal(0, summary.Completed);
        Assert.Equal(0, summary.Pending);
        Assert.False(queue.IsRunning);
        Assert.All(queue.Snapshot(), x => Assert.Equal(QueueJobStatus.Canceled, x.Status));
    }

    [Fact]
    public async Task RunningBatch_RejectsReentrantExecutionAndEnqueue()
    {
        var queue = new ProcessingQueue<string>();
        queue.Enqueue("A", "Active");

        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var running = queue.RunAsync(async (_, _) =>
        {
            entered.SetResult();
            await waiting.Task;
        });

        await entered.Task;

        Assert.True(queue.IsRunning);
        Assert.Throws<InvalidOperationException>(() => queue.Enqueue("B", "Late"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => queue.RunAsync((_, _) => Task.CompletedTask));

        waiting.SetResult();
        await running;
        Assert.False(queue.IsRunning);
    }

    [Fact]
    public void EmptyQueue_ReportsCompletedPercent()
    {
        var queue = new ProcessingQueue<int>();
        var result = queue.GetProgress();
        Assert.Equal(0, result.Total);
        Assert.Equal(100, result.Percent);
    }
}
