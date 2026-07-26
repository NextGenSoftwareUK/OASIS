using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NextGenSoftware.OASIS.ONODE.Service.Platform;

/// <summary>
/// Installs / uninstalls ONODEService as a system daemon.
/// Windows: Windows Service via sc.exe
/// macOS:   LaunchAgent plist in ~/Library/LaunchAgents/
/// Linux:   systemd user unit in ~/.config/systemd/user/
/// </summary>
public static class ServiceInstaller
{
    private const string ServiceName = "ONODEService";
    private const string DisplayName = "ONODE Supervisor Service";
    private const string Description = "Manages Web4–Web10 OASIS API processes and bridges state to OPORTAL.";

    public static void Install()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) InstallWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) InstallMacOS();
        else InstallLinux();
    }

    public static void Uninstall()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) UninstallWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) UninstallMacOS();
        else UninstallLinux();
    }

    // ── Windows ────────────────────────────────────────────────────────────────

    static void InstallWindows()
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path");
        Run("sc.exe", $"create {ServiceName} binPath= \"{exe}\" start= auto DisplayName= \"{DisplayName}\"");
        Run("sc.exe", $"description {ServiceName} \"{Description}\"");
        Run("sc.exe", $"start {ServiceName}");
        Console.WriteLine($"Windows Service '{ServiceName}' installed and started.");
    }

    static void UninstallWindows()
    {
        Run("sc.exe", $"stop {ServiceName}");
        Run("sc.exe", $"delete {ServiceName}");
        Console.WriteLine($"Windows Service '{ServiceName}' removed.");
    }

    // ── macOS ──────────────────────────────────────────────────────────────────

    static void InstallMacOS()
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path");
        var plistDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents");
        Directory.CreateDirectory(plistDir);
        var plistPath = Path.Combine(plistDir, $"one.oasisomniverse.{ServiceName}.plist");

        File.WriteAllText(plistPath, $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
              "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
              <key>Label</key>         <string>one.oasisomniverse.{ServiceName}</string>
              <key>ProgramArguments</key>
              <array><string>{exe}</string></array>
              <key>RunAtLoad</key>     <true/>
              <key>KeepAlive</key>     <true/>
              <key>StandardOutPath</key>
              <string>{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "onode-service.log")}</string>
              <key>StandardErrorPath</key>
              <string>{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "onode-service-error.log")}</string>
            </dict>
            </plist>
            """);

        Run("launchctl", $"load -w \"{plistPath}\"");
        Console.WriteLine($"macOS LaunchAgent installed: {plistPath}");
    }

    static void UninstallMacOS()
    {
        var plistPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents", $"one.oasisomniverse.{ServiceName}.plist");
        if (File.Exists(plistPath))
        {
            Run("launchctl", $"unload \"{plistPath}\"");
            File.Delete(plistPath);
        }
        Console.WriteLine("macOS LaunchAgent removed.");
    }

    // ── Linux ──────────────────────────────────────────────────────────────────

    static void InstallLinux()
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path");
        var unitDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "systemd", "user");
        Directory.CreateDirectory(unitDir);
        var unitPath = Path.Combine(unitDir, $"{ServiceName.ToLower()}.service");

        File.WriteAllText(unitPath, $"""
            [Unit]
            Description={Description}
            After=network.target

            [Service]
            ExecStart={exe}
            Restart=on-failure
            RestartSec=5
            StandardOutput=journal
            StandardError=journal

            [Install]
            WantedBy=default.target
            """);

        Run("systemctl", "--user daemon-reload");
        Run("systemctl", $"--user enable --now {ServiceName.ToLower()}.service");
        Console.WriteLine($"systemd user unit installed: {unitPath}");
    }

    static void UninstallLinux()
    {
        Run("systemctl", $"--user stop {ServiceName.ToLower()}.service");
        Run("systemctl", $"--user disable {ServiceName.ToLower()}.service");
        var unitPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "systemd", "user", $"{ServiceName.ToLower()}.service");
        if (File.Exists(unitPath)) File.Delete(unitPath);
        Run("systemctl", "--user daemon-reload");
        Console.WriteLine("systemd unit removed.");
    }

    static void Run(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args) { UseShellExecute = false };
        using var p = Process.Start(psi);
        p?.WaitForExit();
    }
}
