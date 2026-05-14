using WorkerService1;
using WorkerService1.Configuration;
using WorkerService1.Services;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ZkK40OracleSync";
});

builder.Services
    .AddOptions<DeviceOptions>()
    .Bind(builder.Configuration.GetSection(DeviceOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.IpAddress), "Device:IpAddress is required.")
    .Validate(options => options.Port is > 0 and <= 65535, "Device:Port must be between 1 and 65535.")
    .Validate(options => options.MachineNumber > 0, "Device:MachineNumber must be greater than zero.")
    .Validate(options => options.ReadTimeoutSeconds >= 0, "Device:ReadTimeoutSeconds must be zero or greater.")
    .ValidateOnStart();

builder.Services
    .AddOptions<OracleOptions>()
    .Bind(builder.Configuration.GetSection(OracleOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Oracle:ConnectionString is required.")
    .Validate(options => options.BatchSize > 0, "Oracle:BatchSize must be greater than zero.")
    .Validate(options => options.ProcessedLogCacheSize >= 0, "Oracle:ProcessedLogCacheSize must be zero or greater.")
    .Validate(options => options.CommandTimeoutSeconds >= 0, "Oracle:CommandTimeoutSeconds must be zero or greater.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SyncOptions>()
    .Bind(builder.Configuration.GetSection(SyncOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0, "Sync:IntervalSeconds must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton<IAttendanceDeviceClient, ZkTecoAttendanceDeviceClient>();
builder.Services.AddSingleton<IAttendanceRepository, OracleOdbcAttendanceRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
