namespace Ocyfrovka.Queue;

public sealed record QueueProgress(
    int Total,
    int Completed,
    int Failed,
    int Canceled,
    int Pending,
    string? ActiveItem)
{
    public int Processed => Completed + Failed + Canceled;
    public double Percent => Total == 0 ? 100 : 100.0 * Processed / Total;
}
