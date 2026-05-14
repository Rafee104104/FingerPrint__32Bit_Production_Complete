using Microsoft.Extensions.Options;
using WorkerService1.Configuration;
using WorkerService1.Services;

namespace WorkerService1
{
    public sealed class Worker(
        IAttendanceDeviceClient deviceClient,
        IAttendanceRepository repository,
        IOptions<SyncOptions> options,
        ILogger<Worker> logger) : BackgroundService
    {
        private readonly SyncOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunSyncCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSyncCycleAsync(stoppingToken);
            }
        }

        private async Task RunSyncCycleAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startedAt = TimeProvider.System.GetTimestamp();
                logger.LogInformation("Attendance sync cycle started.");

                var logs = await deviceClient.GetAttendanceLogsAsync(cancellationToken);
                int processedCount = await repository.InsertLogsAsync(logs, cancellationToken);
                var elapsedMs = TimeProvider.System.GetElapsedTime(startedAt).TotalMilliseconds;

                logger.LogInformation(
                    "Attendance sync completed. Fetched={FetchedCount}, processed={ProcessedCount}, elapsed={ElapsedMilliseconds}ms. NextRunInSeconds={IntervalSeconds}.",
                    logs.Count,
                    processedCount,
                    Math.Round(elapsedMs),
                    _options.IntervalSeconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Attendance sync cycle cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance sync cycle failed. The worker will retry on the next interval.");
            }
        }
    }
}
