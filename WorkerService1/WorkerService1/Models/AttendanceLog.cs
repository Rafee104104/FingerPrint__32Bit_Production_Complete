namespace WorkerService1.Models;

public sealed class AttendanceLog
{
    // BS.ATT.USERID: device enroll/user number.
    public long UserId { get; init; }

    // BS.ATT.CHECKTIME: attendance timestamp.
    public DateTime CheckTime { get; init; }
}
