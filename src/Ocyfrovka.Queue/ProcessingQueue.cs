namespace Ocyfrovka.Queue;

/// <summary>
/// Executes a fixed batch of work in FIFO order, at most one item at a time.
/// A failed item does not stop subsequent items. No work is run on a
/// dedicated UI thread; asynchronous workers must honor cancellation.
/// </summary>
public sealed class ProcessingQueue<T>
{
    private sealed class WorkItem
    {
        public required Guid Id { get; init; }
        public required T Payload { get; init; }
        public required string Name { get; init; }
        public QueueJobStatus State { get; set; } = QueueJobStatus.Queued;
        public int Attempts { get; set; }
        public string? Error { get; set; }
    }

    private readonly object _gate = new();
    private readonly List<WorkItem> _items = [];
    private bool _running;

    public bool IsRunning
    {
        get { lock (_gate) return _running; }
    }

    public Guid Enqueue(T value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A queue item name is required.", nameof(displayName));

        lock (_gate)
        {
            if (_running)
                throw new InvalidOperationException("Cannot modify a running batch.");

            var item = new WorkItem
            {
                Id = Guid.NewGuid(),
                Payload = value,
                Name = displayName.Trim()
            };
            _items.Add(item);
            return item.Id;
        }
    }

    public IReadOnlyList<QueueJobSnapshot> Snapshot()
    {
        lock (_gate)
        {
            return _items.Select(x => new QueueJobSnapshot(
                x.Id, x.Name, x.State, x.Attempts, x.Error)).ToArray();
        }
    }

    public QueueProgress GetProgress()
    {
        lock (_gate)
        {
            return SnapshotProgress();
        }
    }

    public int RetryFailed()
    {
        lock (_gate)
        {
            if (_running)
                throw new InvalidOperationException("Cannot retry while processing is running.");

            var count = 0;
            foreach (var item in _items.Where(x => x.State == QueueJobStatus.Failed))
            {
                item.State = QueueJobStatus.Queued;
                item.Error = null;
                item.Attempts = 0;
                count++;
            }
            return count;
        }
    }

    public async Task<QueueProgress> RunAsync(
        Func<T, CancellationToken, Task> worker,
        int maxAttempts = 1,
        IProgress<QueueProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worker);
        if (maxAttempts is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        lock (_gate)
        {
            if (_running)
                throw new InvalidOperationException("This queue is already running.");
            _running = true;
        }

        try
        {
            progress?.Report(GetProgress());

            while (true)
            {
                WorkItem? current;
                lock (_gate)
                {
                    current = _items.FirstOrDefault(x => x.State == QueueJobStatus.Queued);
                    if (current is not null && !cancellationToken.IsCancellationRequested)
                    {
                        current.State = QueueJobStatus.Running;
                        current.Attempts++;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    CancelPending();
                    progress?.Report(GetProgress());
                    break;
                }

                if (current is null)
                    break;

                progress?.Report(GetProgress());

                try
                {
                    await worker(current.Payload, cancellationToken);
                    lock (_gate)
                    {
                        current.State = QueueJobStatus.Completed;
                        current.Error = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    lock (_gate)
                    {
                        current.State = QueueJobStatus.Canceled;
                        current.Error = null;
                    }
                    CancelPending();
                    progress?.Report(GetProgress());
                    break;
                }
                catch (Exception ex)
                {
                    lock (_gate)
                    {
                        current.Error = ex.Message;
                        current.State = current.Attempts < maxAttempts
                            ? QueueJobStatus.Queued
                            : QueueJobStatus.Failed;
                    }

                    // Bounded retry remains on the same item before advancing.
                    // No silent data mutation or auto-retry after maxAttempts.
                    if (current.Attempts < maxAttempts)
                    {
                        lock (_gate)
                        {
                            _items.Remove(current);
                            _items.Insert(0, current);
                        }
                    }
                }

                progress?.Report(GetProgress());
            }

            return GetProgress();
        }
        finally
        {
            lock (_gate)
            {
                _running = false;
            }
        }
    }

    private void CancelPending()
    {
        lock (_gate)
        {
            foreach (var item in _items.Where(x => x.State == QueueJobStatus.Queued))
            {
                item.State = QueueJobStatus.Canceled;
            }
        }
    }

    private QueueProgress SnapshotProgress()
    {
        return new QueueProgress(
            _items.Count,
            _items.Count(x => x.State == QueueJobStatus.Completed),
            _items.Count(x => x.State == QueueJobStatus.Failed),
            _items.Count(x => x.State == QueueJobStatus.Canceled),
            _items.Count(x => x.State is QueueJobStatus.Queued or QueueJobStatus.Running),
            _items.FirstOrDefault(x => x.State == QueueJobStatus.Running)?.Name);
    }
}
