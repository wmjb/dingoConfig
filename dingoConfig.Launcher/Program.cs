// Launcher/Program.cs
using System.Diagnostics;
using System.Runtime.InteropServices;

var baseDir = AppContext.BaseDirectory;

// Start API (always needed)
var apiProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "dingoConfig.Server.dll --urls http://localhost:5000",
        WorkingDirectory = Path.Combine(baseDir, "server"),
        UseShellExecute = false,
        CreateNoWindow = true
    }
};
apiProcess.Start();

Console.WriteLine("API starting...");
await Task.Delay(3000);
Console.WriteLine("API ready at http://localhost:5000\n");

// UI Selection
Console.WriteLine("Select UI:");
Console.WriteLine("1. Blazor Server");
Console.WriteLine("2. React (Vite)");
Console.WriteLine("3. API Only (no UI)");
Console.Write("\nChoice (1-3): ");

var choice = Console.ReadLine();

Process? uiProcess = null;
string? url = null;

switch (choice)
{
    case "1":
        url = "http://localhost:5001";
        uiProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"dingoConfig.Blazor.dll --urls {url}",
                WorkingDirectory = Path.Combine(baseDir, "blazor"),
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        Console.WriteLine("\nStarting Blazor Server...");
        uiProcess.Start();
        break;

    case "2":
        url = "http://localhost:5002";
        uiProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run preview -- --port 5002",
                WorkingDirectory = Path.Combine(baseDir, "react"),
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        Console.WriteLine("\nStarting React UI...");
        uiProcess.Start();
        break;

    case "3":
        Console.WriteLine("\nAPI running at http://localhost:5000");
        Console.WriteLine("Press Ctrl+C to stop...");
        await apiProcess.WaitForExitAsync();
        return;

    default:
        Console.WriteLine("Invalid choice, exiting.");
        apiProcess.Kill();
        return;
}

if (url != null && uiProcess != null)
{
    await Task.Delay(2000);
    OpenBrowser(url);
    Console.WriteLine($"UI opened at {url}");
    Console.WriteLine("Press Ctrl+C to stop...");
    
    await Task.WhenAny(
        apiProcess.WaitForExitAsync(),
        uiProcess.WaitForExitAsync()
    );
}

// Cleanup
apiProcess.Kill();
uiProcess?.Kill();

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }
}