namespace WorkerService1.Models;

public sealed class AttendanceLog
{
    // BS.CARD_ENTRY.D_CARD: DDMMYYYY, example: 25112024
    public required string DCard { get; init; }

    // BS.CARD_ENTRY.T_CARD: HH24MISS, example: 085420
    public required string TCard { get; init; }

    // BS.CARD_ENTRY.CARD_NO: device enroll/user number, example: 114
    public long CardNo { get; init; }

    // BS.CARD_ENTRY.ENTY_DATE: only date part, example: 2024-11-25
    public DateTime EntyDate { get; init; }

    // BS.CARD_ENTRY.CHECKTIME: DDMMYYYYHH24MISS, example: 25112024085420
    public required string CheckTime { get; init; }

    // BS.CARD_ENTRY.MIN1: morning/in punch time, example: 085420
    public string? Min1 { get; init; }

    // BS.CARD_ENTRY.MAX1: evening/out punch time, example: 194157
    public string? Max1 { get; init; }
}
