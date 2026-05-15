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
    // MERGE avoids duplicate insert for same USERID + CHECKTIME.
    private const string InsertSql = """
    MERGE INTO BS.ATT dst
    USING (
        SELECT
            :USERID    AS USERID,
            :CHECKTIME AS CHECKTIME
        FROM DUAL
    ) src
    ON (
        dst.USERID = src.USERID
        AND dst.CHECKTIME = src.CHECKTIME
    )
    WHEN NOT MATCHED THEN
        INSERT (
            USERID,
            CHECKTIME
        )
        VALUES (
            src.USERID,
            src.CHECKTIME
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
                logger.LogWarning(rollbackEx, "Failed to roll back transaction.");
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

        AddNumberArray(command, "USERID", logs, offset, count);
        AddDateArray(command, "CHECKTIME", logs, offset, count);

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
                var key = new AttendanceLogKey(log.UserId, log.CheckTime);
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
                var key = new AttendanceLogKey(log.UserId, log.CheckTime);
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
            values[i] = logs[offset + i].UserId;
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
            values[i] = logs[offset + i].CheckTime;
        }

        var parameter = command.Parameters.Add(name, OracleDbType.Date);
        parameter.Value = values;
    }

    private readonly record struct AttendanceLogKey(long UserId, DateTime CheckTime);
}
