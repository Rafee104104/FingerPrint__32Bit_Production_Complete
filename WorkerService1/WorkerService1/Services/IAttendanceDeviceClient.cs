using WorkerService1.Models;

namespace WorkerService1.Services;

public interface IAttendanceDeviceClient
{
    Task<IReadOnlyList<AttendanceLog>> GetAttendanceLogsAsync(CancellationToken cancellationToken);
}
