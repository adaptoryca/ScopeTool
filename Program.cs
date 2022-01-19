using Firebase.Auth;
using Firebase.Storage;
using System.Management;
using System.Text;

static string WmiRunner(string wmiClass, string[] props, bool newLine = true) {
    return ScopedWmiRunner(null, wmiClass, props, newLine);
}

static string ScopedWmiRunner(string scope, string wmiClass, string[] props, bool newLine = true)
{
    string output = "";
    try
    {
        var search = (String.IsNullOrEmpty(scope)) ? new ManagementObjectSearcher($"select * from {wmiClass}") : new ManagementObjectSearcher(scope, $"select * from {wmiClass}");
        int count = 0;
        foreach (ManagementObject obj in search.Get())
        {
            if (!newLine)
            {
                output += $"{count}: ";
                count++;
            }
            if (obj.Properties.Count <= 0) throw new Exception($"No {wmiClass} objects");
            foreach (var property in obj.Properties)
            {
                if (props.Length > 0 && !props.Contains(property.Name)) continue;
                output += $"{property.Name}: {property.Value}";
                if (newLine) output += "\r\n";
                else output += "; ";
            }
            if (!newLine) output += "\r\n";
        }
    }
    catch (Exception ex)
    {
        output = ex.ToString();
    }
    return output;
}

string sysInfo = WmiRunner("Win32_ComputerSystem", new string[] { "Domain", "Manufacturer", "Model", "PartOfDomain", "UserName", "Name" });
string cpuInfo = WmiRunner("Win32_Processor", new string[] { "Description", "Name", "NumberOfCores", "NumberOfLogicalProcessors" });
string gpuInfo = WmiRunner("Win32_VideoController", new string[] { "Name", "ConfiguredClockSpeed", "Capacity" });
string ramInfo = WmiRunner("win32_PhysicalMemory", new string[] { "PartNumber", "ConfiguredClockSpeed", "Capacity" }, false);
string tpmInfo = ScopedWmiRunner("root\\cimv2\\security\\microsofttpm", "Win32_Tpm", new string[] { });
string mboInfo = WmiRunner("Win32_BaseBoard", new string[] { "Manufacturer", "Product" });
string diskInfo = WmiRunner("Win32_DiskDrive", new string[] { "Model", "Size" });
string netInfo = WmiRunner("Win32_NetworkAdapterConfiguration", new string[] { "Description" });
string osInfo = WmiRunner("Win32_OperatingSystem ", new string[] { "Caption" });

Console.WriteLine("--- System Info: ---");
Console.WriteLine(sysInfo);
Console.WriteLine(osInfo);
Console.WriteLine(cpuInfo);
Console.WriteLine(ramInfo);
Console.WriteLine(gpuInfo);
Console.WriteLine(tpmInfo);
Console.WriteLine(mboInfo);
Console.WriteLine(diskInfo);
Console.WriteLine(netInfo);

Console.WriteLine("Compiling Data...");

try
{
    Console.WriteLine("Compiling Data...");

    string compiledData = $"--- System Info: ---\r\n{sysInfo}\r\n{osInfo}\r\n" +
    $"\r\n--- Hardware Info: ---\r\n{cpuInfo}\r\n{ramInfo}\r\n{gpuInfo}\r\n{mboInfo}\r\n" +
    $"\r\n--- TPM Info: ---\r\n{tpmInfo}\r\n" +
    $"\r\n--- Disk Info: ---\r\n{diskInfo}\r\n" +
    $"\r\n--- Net Info: ---\r\n{netInfo}\r\n";

    Console.WriteLine("Sending Data...");
    byte[] dataArray = Encoding.ASCII.GetBytes(compiledData);
    MemoryStream dataStream = new MemoryStream(dataArray);
    var authProvider = new FirebaseAuthProvider(new FirebaseConfig(""));
    var auth = await authProvider.SignInAnonymouslyAsync();
    var task = new FirebaseStorage(
        "",
        new FirebaseStorageOptions
        {
            AuthTokenAsyncFactory = () => Task.FromResult(auth.FirebaseToken),
            ThrowOnCancel = true,
        }
        ).Child("specs")
        .Child($"{System.DateTime.Now}-{System.Guid.NewGuid()}.txt")
        .PutAsync(dataStream);
    task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");
    var downloadUrl = await task;

    Console.WriteLine($"Saved to: {downloadUrl}");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();