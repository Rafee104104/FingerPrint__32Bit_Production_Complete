namespace WorkerService1.Configuration;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    public int IntervalSeconds { get; set; } = 60;
}
