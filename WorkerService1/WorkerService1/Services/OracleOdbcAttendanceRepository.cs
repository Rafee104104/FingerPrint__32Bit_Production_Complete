using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using WorkerService1.Configuration;
using WorkerService1.Models;

namespace WorkerService1.Services;

// Class name kept same so Program.cs / DI registration does not need to change.
// This repository uses Oracle.ManagedDataAccess directly, not ODBC/DSN.
public sealed class OracleOdbcAttendanceRepository(
    IOptions<OracleOptions> options,
    ILogger<OracleOdbcAttendanceRepository> logger) : IAttendanceRepository
{
    // MERGE avoids duplicate insert for same CARD_NO + CHECKTIME.
    private const string InsertSql = """
    MERGE INTO BS.CARD_ENTRY dst
    USING (
        SELECT
            :D_CARD    AS D_CARD,
            :T_CARD    AS T_CARD,
            :CARD_NO   AS CARD_NO,
            :ENTY_DATE AS ENTY_DATE,
            :CHECKTIME AS CHECKTIME,
            :MIN1      AS MIN1,
            :MAX1      AS MAX1
        FROM DUAL
    ) src
    ON (
        dst.CARD_NO = src.CARD_NO
        AND dst.CHECKTIME = src.CHECKTIME
    )
    WHEN NOT MATCHED THEN
        INSERT (
            D_CARD,
            T_CARD,
            CARD_NO,
            ENTY_DATE,
            CHECKTIME,
            MIN1,
            MAX1
        )
        VALUES (
            src.D_CARD,
            src.T_CARD,
            src.CARD_NO,
            src.ENTY_DATE,
            src.CHECKTIME,
            src.MIN1,
            src.MAX1
        )
    """;

    private readonly string _connectionString = GetConnectionString(options.Value);
    private readonly int _batchSize = Math.Clamp(options.Value.BatchSize, 1, 1_000);
    private readonly int _processedLogCacheSize = Math.Max(0, options.Value.ProcessedLogCacheSize);
    private readonly int _commandTimeoutSeconds = Math.Max(0, options.Value.CommandTimeoutSeconds);
    private readonly object _processedLogKeysLock = new();
    private readonly HashSet<AttendanceLogKey> _processedLogKeys = [];
    private readonly Queue<AttendanceLogKey> _processedLogKeyOrder = [];

    public async Task<int> InsertLogsAsync(IReadOnlyList<AttendanceLog> logs, CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            logger.LogInformation("No attendance logs were fetched from the device; Oracle insert skipped.");
            return 0;
        }

        var pendingLogs = GetPendingLogs(logs);
        if (pendingLogs.Count == 0)
        {
            logger.LogInformation(
                "Fetched {FetchedLogCount} attendance logs, but all are already in the processed cache.",
                logs.Count);
            return 0;
        }

        logger.LogInformation(
            "Opening Oracle connection for {PendingLogCount} attendance logs. BatchSize={BatchSize}, ProcessedLogCacheSize={ProcessedLogCacheSize}.",
            pendingLogs.Count,
            _batchSize,
            _processedLogCacheSize);

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var processedCount = 0;

        try
        {
            for (var offset = 0; offset < pendingLogs.Count; offset += _batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchCount = Math.Min(_batchSize, pendingLogs.Count - offset);
                await ExecuteBatchAsync(
                    connection,
                    transaction,
                    pendingLogs,
                    offset,
                    batchCount,
                    cancellationToken);
                processedCount += batchCount;
            }

            transaction.Commit();
            CacheProcessedLogs(pendingLogs);

            logger.LogInformation(
                "Oracle merge completed. Fetched={FetchedLogCount}, Pending={PendingLogCount}, Processed={ProcessedCount}.",
                logs.Count,
                pendingLogs.Count,
                processedCount);

            return processedCount;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                logger.LogWarning(rollbackEx, "Failed to roll back BS.CARD_ENTRY transaction.");
            }

            throw;
        }
    }

    private async Task ExecuteBatchAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        IReadOnlyList<AttendanceLog> logs,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandType = CommandType.Text;
        command.CommandText = InsertSql;
        command.ArrayBindCount = count;
        if (_commandTimeoutSeconds > 0)
        {
            command.CommandTimeout = _commandTimeoutSeconds;
        }

        AddVarcharArray(command, "D_CARD", logs, offset, count, 50, static log => log.DCard);
        AddVarcharArray(command, "T_CARD", logs, offset, count, 50, static log => log.TCard);
        AddNumberArray(command, "CARD_NO", logs, offset, count);
        AddDateArray(command, "ENTY_DATE", logs, offset, count);
        AddVarcharArray(command, "CHECKTIME", logs, offset, count, 50, static log => log.CheckTime);
        AddVarcharArray(command, "MIN1", logs, offset, count, 12, static log => log.Min1);
        AddVarcharArray(command, "MAX1", logs, offset, count, 15, static log => log.Max1);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private List<AttendanceLog> GetPendingLogs(IReadOnlyList<AttendanceLog> logs)
    {
        var pendingLogs = new List<AttendanceLog>(logs.Count);
        var pendingLogKeys = new HashSet<AttendanceLogKey>();

        lock (_processedLogKeysLock)
        {
            foreach (var log in logs)
            {
                var key = new AttendanceLogKey(log.CardNo, log.CheckTime);
                if (_processedLogKeys.Contains(key) || !pendingLogKeys.Add(key))
                {
                    continue;
                }

                pendingLogs.Add(log);
            }
        }

        return pendingLogs;
    }

    private void CacheProcessedLogs(IReadOnlyList<AttendanceLog> logs)
    {
        if (_processedLogCacheSize == 0)
        {
            return;
        }

        lock (_processedLogKeysLock)
        {
            foreach (var log in logs)
            {
                var key = new AttendanceLogKey(log.CardNo, log.CheckTime);
                if (!_processedLogKeys.Add(key))
                {
                    continue;
                }

                _processedLogKeyOrder.Enqueue(key);
                while (_processedLogKeys.Count > _processedLogCacheSize &&
                       _processedLogKeyOrder.TryDequeue(out var keyToRemove))
                {
                    _processedLogKeys.Remove(keyToRemove);
                }
            }
        }
    }

    private static string GetConnectionString(OracleOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ConnectionString)
            ? throw new InvalidOperationException("Oracle:ConnectionString is required.")
            : options.ConnectionString.Trim();
    }

    private static void AddVarcharArray(
        OracleCommand command,
        string name,
        IReadOnlyList<AttendanceLog> logs,
        int offset,
        int count,
        int size,
        Func<AttendanceLog, string?> selector)
    {
        var values = new string[count];
        OracleParameterStatus[]? statuses = null;

        for (var i = 0; i < count; i++)
        {
            var value = selector(logs[offset + i])?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                values[i] = string.Empty;
                statuses ??= CreateSuccessStatuses(count);
                statuses[i] = OracleParameterStatus.NullInsert;
                continue;
            }

            values[i] = value;
        }

        var parameter = command.Parameters.Add(name, OracleDbType.Varchar2, size);
        parameter.Value = values;
        if (statuses is not null)
        {
            parameter.ArrayBindStatus = statuses;
        }
    }

    private static void AddNumberArray(
        OracleCommand command,
        string name,
        IReadOnlyList<AttendanceLog> logs,
        int offset,
        int count)
    {
        var values = new decimal[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = logs[offset + i].CardNo;
        }

        var parameter = command.Parameters.Add(name, OracleDbType.Decimal);
        parameter.Value = values;
    }

    private static void AddDateArray(
        OracleCommand command,
        string name,
        IReadOnlyList<AttendanceLog> logs,
        int offset,
        int count)
    {
        var values = new DateTime[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = logs[offset + i].EntyDate.Date;
        }

        var parameter = command.Parameters.Add(name, OracleDbType.Date);
        parameter.Value = values;
    }

    private static OracleParameterStatus[] CreateSuccessStatuses(int count)
    {
        var statuses = new OracleParameterStatus[count];
        Array.Fill(statuses, OracleParameterStatus.Success);
        return statuses;
    }

    private readonly record struct AttendanceLogKey(long CardNo, string CheckTime);
}
