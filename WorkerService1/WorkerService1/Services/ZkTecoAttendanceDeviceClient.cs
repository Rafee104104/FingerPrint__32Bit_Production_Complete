using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkerService1.Configuration;
using WorkerService1.Models;

namespace WorkerService1.Services;

public sealed class ZkTecoAttendanceDeviceClient(
    IOptions<DeviceOptions> options,
    ILogger<ZkTecoAttendanceDeviceClient> logger) : IAttendanceDeviceClient
{
    private const string ProgId = "zkemkeeper.ZKEM";
    private const string RegSvr32Path = @"C:\Windows\SysWOW64\regsvr32.exe";
    private const int DefaultLogCapacity = 1_024;
    private const int ProgressLogInterval = 10_000;

    private readonly DeviceOptions _options = Validate(options.Value);
    private readonly SemaphoreSlim _readLock = new(1, 1);
    private AttendanceReadMode _readMode = AttendanceReadMode.Unknown;
    private LegacyLogVariant? _legacyLogVariant;

    private enum AttendanceReadMode
    {
        Unknown,
        Ssr,
        Legacy
    }

    private readonly record struct LegacyLogVariant(
        int ArgumentCount,
        int UserIdIndex,
        int YearIndex,
        int MonthIndex,
        int DayIndex,
        int HourIndex,
        int MinuteIndex,
        int? SecondIndex);

    private static readonly LegacyLogVariant[] LegacyLogVariants =
    [
        new(
            ArgumentCount: 11,
            UserIdIndex: 2,
            YearIndex: 6,
            MonthIndex: 7,
            DayIndex: 8,
            HourIndex: 9,
            MinuteIndex: 10,
            SecondIndex: null),
        new(
            ArgumentCount: 10,
            UserIdIndex: 1,
            YearIndex: 4,
            MonthIndex: 5,
            DayIndex: 6,
            HourIndex: 7,
            MinuteIndex: 8,
            SecondIndex: 9)
    ];

    public async Task<IReadOnlyList<AttendanceLog>> GetAttendanceLogsAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("ZKTeco COM integration requires Windows.");
        }

        if (Environment.Is64BitProcess)
        {
            throw new PlatformNotSupportedException(
                $"Current process is {RuntimeInformation.ProcessArchitecture}. The registered ZKTeco COM SDK requires the Worker Service to run as a 32-bit/x86 process. Build and run the Worker Service as x86/win-x86.");
        }

        if (!await _readLock.WaitAsync(0, cancellationToken))
        {
            logger.LogWarning("Previous ZKTeco attendance read is still running; skipping this sync cycle.");
            return Array.Empty<AttendanceLog>();
        }

        Task<IReadOnlyList<AttendanceLog>>? readTask = null;

        try
        {
            logger.LogInformation(
                "Starting ZKTeco attendance read from {IpAddress}:{Port}. MachineNumber={MachineNumber}, ReadTimeoutSeconds={ReadTimeoutSeconds}.",
                _options.IpAddress,
                _options.Port,
                _options.MachineNumber,
                _options.ReadTimeoutSeconds);

            readTask = ExecuteOnStaThreadAsync(
                () => ReadLogs(cancellationToken),
                cancellationToken);

            if (_options.ReadTimeoutSeconds == 0)
            {
                return await readTask;
            }

            return await readTask.WaitAsync(
                TimeSpan.FromSeconds(_options.ReadTimeoutSeconds),
                cancellationToken);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(
                $"ZKTeco attendance read timed out after {_options.ReadTimeoutSeconds} seconds. Check device IP/port, network, SDK registration, or increase Device:ReadTimeoutSeconds.",
                ex);
        }
        finally
        {
            if (readTask is null || readTask.IsCompleted)
            {
                _readLock.Release();
            }
            else
            {
                _ = readTask.ContinueWith(
                    static (_, state) => ((SemaphoreSlim)state!).Release(),
                    _readLock,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    private IReadOnlyList<AttendanceLog> ReadLogs(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Type comType = Type.GetTypeFromProgID(ProgId, throwOnError: false)
            ?? throw new InvalidOperationException(GetRegistrationErrorMessage());

        object deviceObject = Activator.CreateInstance(comType)
            ?? throw new InvalidOperationException("Failed to create the ZKTeco ZKEM COM object.");

        dynamic device = deviceObject;
        var connected = false;

        try
        {
            logger.LogInformation(
                "Connecting to ZKTeco device at {IpAddress}:{Port} with machine number {MachineNumber}.",
                _options.IpAddress,
                _options.Port,
                _options.MachineNumber);

            connected = Convert.ToBoolean(device.Connect_Net(_options.IpAddress, _options.Port), CultureInfo.InvariantCulture);
            if (!connected)
            {
                throw new InvalidOperationException(
                    $"Could not connect to ZKTeco device at {_options.IpAddress}:{_options.Port}. {GetLastErrorMessage(deviceObject)}");
            }

            logger.LogInformation("Connected to ZKTeco device. Disabling device during attendance read.");

            var disabled = Convert.ToBoolean(device.EnableDevice(_options.MachineNumber, false), CultureInfo.InvariantCulture);
            if (!disabled)
            {
                logger.LogWarning(
                    "EnableDevice({MachineNumber}, false) returned false. Continuing with log read attempt.",
                    _options.MachineNumber);
            }

            var readStarted = Convert.ToBoolean(
                device.ReadGeneralLogData(_options.MachineNumber),
                CultureInfo.InvariantCulture);

            if (!readStarted)
            {
                logger.LogWarning(
                    "ReadGeneralLogData({MachineNumber}) returned false. {LastErrorMessage}",
                    _options.MachineNumber,
                    GetLastErrorMessage(deviceObject));

                return Array.Empty<AttendanceLog>();
            }

            IReadOnlyList<AttendanceLog> ssrLogs;
            if (_readMode != AttendanceReadMode.Legacy &&
                TryReadSsrLogs(deviceObject, cancellationToken, out ssrLogs))
            {
                _readMode = AttendanceReadMode.Ssr;
                logger.LogInformation("Fetched {LogCount} attendance logs using SSR_GetGeneralLogData.", ssrLogs.Count);

                return ssrLogs;
            }

            _readMode = AttendanceReadMode.Legacy;
            var legacyLogs = ReadLegacyLogs(deviceObject, cancellationToken);
            logger.LogInformation("Fetched {LogCount} attendance logs using GetGeneralLogData fallback.", legacyLogs.Count);

            return legacyLogs;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
        {
            throw new InvalidOperationException(GetRegistrationErrorMessage(), ex);
        }
        finally
        {
            if (connected)
            {
                TryEnableDevice(device, enabled: true);
                TryDisconnect(device);
            }

            ReleaseComObject(deviceObject);
        }
    }

    private bool TryReadSsrLogs(object deviceObject, CancellationToken cancellationToken, out IReadOnlyList<AttendanceLog> logs)
    {
        dynamic device = deviceObject;
        var results = new List<AttendanceLog>(DefaultLogCapacity);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string enrollNumber = string.Empty;
                int verifyMode = 0;
                int inOutMode = 0;
                int year = 0;
                int month = 0;
                int day = 0;
                int hour = 0;
                int minute = 0;
                int second = 0;
                int workCode = 0;

                var hasRecord = Convert.ToBoolean(
                    device.SSR_GetGeneralLogData(
                        _options.MachineNumber,
                        out enrollNumber,
                        out verifyMode,
                        out inOutMode,
                        out year,
                        out month,
                        out day,
                        out hour,
                        out minute,
                        out second,
                        ref workCode),
                    CultureInfo.InvariantCulture);

                if (!hasRecord)
                {
                    logs = results;
                    return true;
                }

                if (TryCreateLog(enrollNumber, year, month, day, hour, minute, second, out var log))
                {
                    results.Add(log);
                }
            }
        }
        catch (Exception ex) when (IsMissingMemberException(ex))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    ex,
                    "SSR_GetGeneralLogData is not available on this device/SDK registration. Falling back to GetGeneralLogData.");
            }

            logs = Array.Empty<AttendanceLog>();
            return false;
        }
    }

    private IReadOnlyList<AttendanceLog> ReadLegacyLogs(object deviceObject, CancellationToken cancellationToken)
    {
        var cachedVariant = _legacyLogVariant;
        if (cachedVariant.HasValue &&
            TryReadLegacyVariant(deviceObject, cancellationToken, cachedVariant.Value, out var logs))
        {
            return logs;
        }

        foreach (var variant in LegacyLogVariants)
        {
            if (cachedVariant.HasValue && variant.Equals(cachedVariant.Value))
            {
                continue;
            }

            if (TryReadLegacyVariant(deviceObject, cancellationToken, variant, out logs))
            {
                _legacyLogVariant = variant;
                return logs;
            }
        }

        throw new InvalidOperationException(
            "The registered ZKTeco SDK does not expose a supported GetGeneralLogData method. Verify the x86 SDK registration.");
    }

    private bool TryReadLegacyVariant(
        object deviceObject,
        CancellationToken cancellationToken,
        LegacyLogVariant variant,
        out IReadOnlyList<AttendanceLog> logs)
    {
        var results = new List<AttendanceLog>(DefaultLogCapacity);
        var templateArguments = CreateLegacyArguments(variant.ArgumentCount, _options.MachineNumber);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var arguments = (object?[])templateArguments.Clone();
                var hasRecord = InvokeComMethod<bool>(deviceObject, "GetGeneralLogData", arguments);

                if (!hasRecord)
                {
                    logs = results;
                    return true;
                }

                var userId = Convert.ToString(arguments[variant.UserIdIndex], CultureInfo.InvariantCulture);
                var year = ConvertToInt(arguments[variant.YearIndex]);
                var month = ConvertToInt(arguments[variant.MonthIndex]);
                var day = ConvertToInt(arguments[variant.DayIndex]);
                var hour = ConvertToInt(arguments[variant.HourIndex]);
                var minute = ConvertToInt(arguments[variant.MinuteIndex]);
                var second = variant.SecondIndex.HasValue ? ConvertToInt(arguments[variant.SecondIndex.Value]) : 0;

                if (TryCreateLog(userId, year, month, day, hour, minute, second, out var log))
                {
                    results.Add(log);
                }
            }
        }
        catch (Exception ex) when (IsMissingMemberException(ex))
        {
            logs = Array.Empty<AttendanceLog>();
            return false;
        }
    }

    private static object?[] CreateLegacyArguments(int argumentCount, int machineNumber)
    {
        var arguments = new object?[argumentCount];
        Array.Fill<object?>(arguments, 0);
        arguments[0] = machineNumber;
        return arguments;
    }

    private bool TryCreateLog(
        string? rawCardNo,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        out AttendanceLog log)
    {
        log = default!;

        if (!TryNormalizeCardNo(rawCardNo, out var cardNo))
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Skipping attendance log with invalid CARD_NO value: {RawCardNo}.", rawCardNo);
            }

            return false;
        }

        try
        {
            var checkDateTime = new DateTime(year, month, day, hour, minute, second);
            var dCard = FormatDate(checkDateTime);
            var tCard = FormatTime(checkDateTime);
            var checkTime = string.Concat(dCard, tCard);

            // Current rule: before 12:00:00 = MIN1, 12:00:00 or later = MAX1.
            // If your office rule is different, change only this line.
            var isMinPunch = checkDateTime.TimeOfDay < TimeSpan.FromHours(12);

            log = new AttendanceLog
            {
                DCard = dCard,
                TCard = tCard,
                CardNo = cardNo,
                EntyDate = checkDateTime.Date,
                CheckTime = checkTime,
                Min1 = isMinPunch ? tCard : null,
                Max1 = isMinPunch ? null : tCard
            };

            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    ex,
                    "Skipping invalid attendance timestamp from device for CARD_NO {CardNo}: {Year}-{Month}-{Day} {Hour}:{Minute}:{Second}.",
                    cardNo,
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second);
            }

            return false;
        }
    }

    private static string FormatDate(DateTime value)
    {
        return string.Create(8, value, static (destination, dateTime) =>
        {
            WriteTwoDigits(destination, 0, dateTime.Day);
            WriteTwoDigits(destination, 2, dateTime.Month);
            WriteFourDigits(destination, 4, dateTime.Year);
        });
    }

    private static string FormatTime(DateTime value)
    {
        return string.Create(6, value, static (destination, dateTime) =>
        {
            WriteTwoDigits(destination, 0, dateTime.Hour);
            WriteTwoDigits(destination, 2, dateTime.Minute);
            WriteTwoDigits(destination, 4, dateTime.Second);
        });
    }

    private static void WriteTwoDigits(Span<char> destination, int offset, int value)
    {
        destination[offset] = (char)('0' + (value / 10));
        destination[offset + 1] = (char)('0' + (value % 10));
    }

    private static void WriteFourDigits(Span<char> destination, int offset, int value)
    {
        destination[offset] = (char)('0' + (value / 1_000));
        destination[offset + 1] = (char)('0' + (value / 100 % 10));
        destination[offset + 2] = (char)('0' + (value / 10 % 10));
        destination[offset + 3] = (char)('0' + (value % 10));
    }

    private string GetLastErrorMessage(object deviceObject)
    {
        try
        {
            object?[] arguments = { 0 };
            var hasError = InvokeComMethod<bool>(deviceObject, "GetLastError", arguments);
            if (hasError)
            {
                return $"SDK error code: {ConvertToInt(arguments[0])}.";
            }
        }
        catch (Exception ex) when (IsMissingMemberException(ex))
        {
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to read the last ZKTeco SDK error code.");
        }

        return "SDK did not provide an error code.";
    }

    private void TryEnableDevice(dynamic device, bool enabled)
    {
        try
        {
            var result = Convert.ToBoolean(device.EnableDevice(_options.MachineNumber, enabled), CultureInfo.InvariantCulture);
            if (!result)
            {
                logger.LogWarning(
                    "EnableDevice({MachineNumber}, {Enabled}) returned false during cleanup.",
                    _options.MachineNumber,
                    enabled);
            }
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException)
        {
            logger.LogWarning(
                ex,
                "Failed to set device enabled state to {Enabled} for machine number {MachineNumber}.",
                enabled,
                _options.MachineNumber);
        }
    }

    private void TryDisconnect(dynamic device)
    {
        try
        {
            device.Disconnect();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Disconnected from ZKTeco device.");
            }
        }
        catch (Exception ex) when (ex is COMException or RuntimeBinderException)
        {
            logger.LogWarning(ex, "Failed to disconnect cleanly from ZKTeco device.");
        }
    }

    private void ReleaseComObject(object deviceObject)
    {
        if (!Marshal.IsComObject(deviceObject))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(deviceObject);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to release the ZKTeco COM object.");
        }
    }

    private static T InvokeComMethod<T>(object deviceObject, string methodName, object?[]? arguments = null)
    {
        var result = deviceObject.GetType().InvokeMember(
            methodName,
            BindingFlags.InvokeMethod,
            binder: null,
            target: deviceObject,
            args: arguments);

        return result switch
        {
            T value => value,
            null when typeof(T) == typeof(bool) => (T)(object)false,
            null => throw new InvalidOperationException($"COM method {methodName} returned null."),
            _ => (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture)
        };
    }

    private static bool IsMissingMemberException(Exception ex)
    {
        var candidate = ex is TargetInvocationException { InnerException: not null }
            ? ex.InnerException
            : ex;

        if (candidate is MissingMemberException or MissingMethodException or RuntimeBinderException)
        {
            return true;
        }

        return candidate is COMException comException
               && comException.ErrorCode == unchecked((int)0x80020003);
    }

    private static Task<T> ExecuteOnStaThreadAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled(cancellationToken);
                    return;
                }

                completionSource.TrySetResult(operation());
            }
            catch (OperationCanceledException ex)
            {
                completionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ZKTecoAttendanceReader"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completionSource.Task;
    }

    private static DeviceOptions Validate(DeviceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.IpAddress))
        {
            throw new InvalidOperationException("Device:IpAddress is required.");
        }

        if (options.Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Device:Port must be between 1 and 65535.");
        }

        if (options.MachineNumber <= 0)
        {
            throw new InvalidOperationException("Device:MachineNumber must be greater than zero.");
        }

        return options;
    }

    private static string GetRegistrationErrorMessage()
    {
        var localSdkPath = Path.Combine(AppContext.BaseDirectory, "zkemkeeper.dll");
        var sdkPath = File.Exists(localSdkPath)
            ? localSdkPath
            : @"C:\Windows\SysWOW64\zkemkeeper.dll";

        return $"ZKTeco COM SDK is not registered for this process (ProcessArchitecture={RuntimeInformation.ProcessArchitecture}). Register the 32-bit zkemkeeper.dll from an elevated PowerShell/CMD with: \"{RegSvr32Path}\" \"{sdkPath}\".";
    }

    private static int ConvertToInt(object? value)
    {
        return value switch
        {
            null => 0,
            int intValue => intValue,
            short shortValue => shortValue,
            long longValue => checked((int)longValue),
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };
    }

    private static bool TryNormalizeCardNo(string? rawCardNo, out long cardNo)
    {
        cardNo = 0;

        if (string.IsNullOrWhiteSpace(rawCardNo))
        {
            return false;
        }

        var normalized = rawCardNo.Trim();
        return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out cardNo);
    }
}
