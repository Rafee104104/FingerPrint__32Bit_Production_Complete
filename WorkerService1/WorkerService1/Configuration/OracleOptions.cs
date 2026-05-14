namespace WorkerService1.Configuration;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string ConnectionString { get; set; } = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522)))(CONNECT_DATA=(SERVICE_NAME=ORCLBS)));User Id=BS;Password=beta8090;";

    public int BatchSize { get; set; } = 500;

    public int ProcessedLogCacheSize { get; set; } = 1_000_000;

    public int CommandTimeoutSeconds { get; set; } = 120;
}
