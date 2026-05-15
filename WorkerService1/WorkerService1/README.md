# ZKTeco K40 to Oracle Worker Service

Windows-only x86 .NET 10 Worker Service that reads attendance logs from a ZKTeco K40 device through the 32-bit ZKTeco COM SDK and writes them into Oracle through `Oracle.ManagedDataAccess.Core`.

## Deployment Checklist

1. Confirm the device network connection:

   ```powershell
   Test-NetConnection 192.168.88.101 -Port 4370
   ```

2. Confirm the Oracle listener connection:

   ```powershell
   Test-NetConnection localhost -Port 1522
   ```

3. Create `BS.ATT` using `database.sql`.

4. Configure `WorkerService1\appsettings.json` or set machine-level environment variables. The service requires:

   ```json
   {
     "Device": {
       "IpAddress": "192.168.88.101",
       "Port": 4370,
       "MachineNumber": 1,
       "ReadTimeoutSeconds": 300
     },
     "Oracle": {
       "ConnectionString": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522)))(CONNECT_DATA=(SERVICE_NAME=ORCLBS)));User Id=BS;Password=beta8090;",
       "BatchSize": 500,
       "ProcessedLogCacheSize": 1000000,
       "CommandTimeoutSeconds": 120
     },
     "Sync": {
       "IntervalSeconds": 60
     }
   }
   ```

   Environment variable override example:

   ```powershell
   [Environment]::SetEnvironmentVariable(
       'Oracle__ConnectionString',
       'Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522)))(CONNECT_DATA=(SERVICE_NAME=ORCLBS)));User Id=BS;Password=beta8090;',
       'Machine')
   ```

5. Build:

   ```powershell
   dotnet restore .\WorkerService1.slnx
   dotnet build .\WorkerService1\WorkerService1.csproj -c Release
   ```

6. Publish a self-contained x86 service package:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish.ps1
   ```

   To include the ZKTeco SDK DLL beside the service executable:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish.ps1 -ZkSdkDll "D:\Deploy\ZKTecoSDK\32bit\zkemkeeper.dll"
   ```

7. Install or update the Windows Service from an elevated PowerShell:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\install-service.ps1 -Start
   ```

   If the SDK DLL was not included during publish:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\install-service.ps1 -ZkSdkDll "D:\Deploy\ZKTecoSDK\32bit\zkemkeeper.dll" -Start
   ```

8. Check service status and logs:

   ```powershell
   Get-Service ZkK40OracleSync
   Get-EventLog -LogName Application -Source ZkK40OracleSync -Newest 20
   ```

9. Uninstall if needed:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\uninstall-service.ps1
   ```

## Manual Commands

Publish without the helper script:

```powershell
dotnet publish .\WorkerService1\WorkerService1.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\publish\ZkK40OracleSync
```

Register the 32-bit ZKTeco SDK manually from an elevated PowerShell:

```powershell
C:\Windows\SysWOW64\regsvr32.exe "C:\Services\ZkK40OracleSync\zkemkeeper.dll"
```

## Notes

- The executable and Windows Service name are `ZkK40OracleSync`.
- The service never clears attendance logs from the K40 and does not delete machine data.
- Duplicate records are skipped by matching `BS.ATT.USERID` (`NUMBER(10)`) and `BS.ATT.CHECKTIME`.
- Inserts use Oracle array binding in batches, and the worker caches processed punch keys in memory to avoid resending the same device history every cycle.
- This project runs as x86 because the configured ZKTeco COM SDK is 32-bit.
