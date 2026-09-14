using System;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

internal static class Program
{
    static int Main()
    {
        Console.Title = "School PC Authorized Remote Setup";

        Console.WriteLine("==============================================");
        Console.WriteLine("   AUTHORIZED SCHOOL PC REMOTE SETUP");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        if (!IsAdministrator())
        {
            Console.WriteLine("ERROR: Please right-click the EXE and choose");
            Console.WriteLine("\"Run as administrator\".");
            Pause();
            return 1;
        }

        string edition = GetWindowsEdition();
        Console.WriteLine($"Windows edition: {edition}");

        if (edition.Contains("Home", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine("Windows Home cannot host incoming Microsoft Remote Desktop.");
            Console.WriteLine("No RDP changes were made.");
            Pause();
            return 1;
        }

        try
        {
            Console.WriteLine();
            Console.WriteLine("[1/4] Enabling Remote Desktop...");
            RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name fDenyTSConnections -Type DWord -Value 0");

            Console.WriteLine("[2/4] Requiring Network Level Authentication...");
            RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' -Name UserAuthentication -Type DWord -Value 1");

            Console.WriteLine("[3/4] Enabling Remote Desktop firewall rules...");
            RunPowerShell(@"Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'");

            Console.WriteLine("[4/4] Enabling Wake-on-Magic-Packet where supported...");
            RunPowerShell(@"$a=Get-NetAdapter -Physical -ErrorAction SilentlyContinue; foreach($n in $a){try{Set-NetAdapterPowerManagement -Name $n.Name -WakeOnMagicPacket Enabled -ErrorAction Stop; Write-Output ('Enabled: '+$n.Name)}catch{Write-Output ('Skipped: '+$n.Name)}}");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            string infoFile = System.IO.Path.Combine(desktop, "School PC Remote Info.txt");

            string info = GetConnectionInfo(edition);
            System.IO.File.WriteAllText(infoFile, info);

            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine("SETUP COMPLETE");
            Console.WriteLine("==============================================");
            Console.WriteLine($"Connection information saved to:");
            Console.WriteLine(infoFile);
            Console.WriteLine();
            Console.WriteLine("Important:");
            Console.WriteLine("- RDP requires the PC to be powered on.");
            Console.WriteLine("- BIOS/UEFI may still need Wake-on-LAN enabled.");
            Console.WriteLine("- Do not expose RDP port 3389 directly to the Internet.");
            Console.WriteLine("- Use an administrator-approved VPN/private network for remote access.");
            Pause();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("SETUP FAILED:");
            Console.WriteLine(ex.Message);
            Pause();
            return 1;
        }
    }

    static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static string GetWindowsEdition()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

        return key?.GetValue("ProductName")?.ToString() ?? "Unknown";
    }

    static void RunPowerShell(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                        "\"" + command.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using Process process = Process.Start(psi)
            ?? throw new Exception("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine(output.Trim());

        if (process.ExitCode != 0)
            throw new Exception(string.IsNullOrWhiteSpace(error)
                ? $"PowerShell command failed with exit code {process.ExitCode}."
                : error.Trim());
    }

    static string GetConnectionInfo(string edition)
    {
        string computer = Environment.MachineName;
        string ips = RunPowerShellForText(
            "(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | " +
            "Where-Object {$_.IPAddress -notmatch '^127\\.' -and $_.IPAddress -notmatch '^169\\.254\\.'} | " +
            "Select-Object -ExpandProperty IPAddress) -join [Environment]::NewLine");

        string macs = RunPowerShellForText(
            "(Get-NetAdapter -Physical -ErrorAction SilentlyContinue | " +
            "Select-Object Name,MacAddress,Status | Out-String)");

        return
$@"AUTHORIZED REMOTE ACCESS
========================

Computer name:
{computer}

Windows edition:
{edition}

Current IPv4 addresses:
{ips}

Network adapters / MAC addresses:
{macs}

Remote Desktop:
Enabled

Network Level Authentication:
Enabled

Wake-on-Magic-Packet:
Configured where Windows supports it

IMPORTANT:
- The PC must be powered on for RDP.
- BIOS/UEFI may need Wake-on-LAN enabled manually.
- Remote Wake-on-LAN across the Internet normally needs an approved VPN/router/device on the same network.
- Do not expose TCP 3389 directly to the public Internet.
- Use this only on a PC and network you are authorized to administer.
";
    }

    static string RunPowerShellForText(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                        "\"" + command.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using Process process = Process.Start(psi)
            ?? throw new Exception("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(string.IsNullOrWhiteSpace(error)
                ? $"PowerShell command failed with exit code {process.ExitCode}."
                : error.Trim());

        return output.Trim();
    }

    static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Press Enter to close.");
        Console.ReadLine();
    }
}
