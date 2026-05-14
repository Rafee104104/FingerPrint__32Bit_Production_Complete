namespace WorkerService1.Configuration;

public sealed class DeviceOptions
{
    public const string SectionName = "Device";

    public string IpAddress { get; set; } = "192.168.88.101";

    public int Port { get; set; } = 4370;

    public int MachineNumber { get; set; } = 1;

    public int ReadTimeoutSeconds { get; set; } = 300;
}
