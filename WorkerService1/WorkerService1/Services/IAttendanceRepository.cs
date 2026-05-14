using WorkerService1.Models;

namespace WorkerService1.Services;

public interface IAttendanceRepository
{
    Task<int> InsertLogsAsync(IReadOnlyList<AttendanceLog> logs, CancellationToken cancellationToken);
}
