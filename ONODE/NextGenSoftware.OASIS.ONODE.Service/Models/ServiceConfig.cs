namespace NextGenSoftware.OASIS.ONODE.Service.Models;

public enum WindowMode { Visible, Hidden, Minimised }
public enum ServiceStatus { NotInstalled, Stopped, Starting, Running, Stopping, Crashed, Degraded }

public class ServiceConfig
{
    public bool Enabled { get; set; } = false;
    public WindowMode WindowMode { get; set; } = WindowMode.Visible;
    public int Port { get; set; }
    public string? OverridePath { get; set; }
    public int CrashRestartDelaySeconds { get; set; } = 5;
    public int MaxRestartAttempts { get; set; } = 3;
    public int RestartWindowMinutes { get; set; } = 10;
}

public class SupervisorConfig
{
    public int Port { get; set; } = 8765;
    public string OASISRootPath { get; set; } = DefaultOASISRootPath();
    public string OASISDNAPath { get; set; } = "";
    public string TokenPath { get; set; } = "";
    public int HolonPushIntervalSeconds { get; set; } = 5;
    public int CommandPollIntervalSeconds { get; set; } = 3;
    public int HealthPollIntervalSeconds { get; set; } = 10;
    public int MetricsPollIntervalSeconds { get; set; } = 5;
    public int LogRingBufferLines { get; set; } = 1000;
    public string AvatarId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public Dictionary<string, ServiceConfig> Services { get; set; } = DefaultServices();
    public Dictionary<string, string> Groups { get; set; } = DefaultGroups();

    public string ResolveOASISDNAPath()
    {
        var envPath = Environment.GetEnvironmentVariable("OASISDNA_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath)) return envPath;
        if (!string.IsNullOrEmpty(OASISDNAPath) && File.Exists(OASISDNAPath)) return OASISDNAPath;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userPath = Path.Combine(home, ".oasis", "OASISDNA.json");
        if (File.Exists(userPath)) return userPath;
        var etcPath = Path.Combine("/etc", "oasis", "OASISDNA.json");
        if (File.Exists(etcPath)) return etcPath;
        // fall back to sibling of executable
        return Path.Combine(AppContext.BaseDirectory, "OASISDNA.json");
    }

    public string ResolveProjectPath(string serviceId)
    {
        if (Services.TryGetValue(serviceId, out var cfg) && !string.IsNullOrEmpty(cfg.OverridePath))
            return cfg.OverridePath;
        return serviceId.ToLower() switch
        {
            "web4"  => Path.Combine(OASISRootPath, "ONODE", "NextGenSoftware.OASIS.API.ONODE.WebAPI"),
            "web5"  => Path.Combine(OASISRootPath, "STAR ODK", "NextGenSoftware.OASIS.STAR.WebAPI"),
            "web6"  => Path.Combine(OASISRootPath, "WEB6",  "NextGenSoftware.OASIS.Web6.WebAPI"),
            "web7"  => Path.Combine(OASISRootPath, "WEB7",  "NextGenSoftware.OASIS.Web7.WebAPI"),
            "web8"  => Path.Combine(OASISRootPath, "WEB8",  "NextGenSoftware.OASIS.Web8.WebAPI"),
            "web9"  => Path.Combine(OASISRootPath, "WEB9",  "NextGenSoftware.OASIS.Web9.WebAPI"),
            "web10" => Path.Combine(OASISRootPath, "WEB10", "NextGenSoftware.OASIS.Web10.WebAPI"),
            _ => ""
        };
    }

    static string DefaultOASISRootPath()
    {
        if (OperatingSystem.IsWindows()) return @"C:\Source\OASIS2";
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "OASIS2");
    }

    static Dictionary<string, ServiceConfig> DefaultServices() => new()
    {
        ["web4"]  = new() { Enabled = true,  Port = 5000, WindowMode = WindowMode.Visible },
        ["web5"]  = new() { Enabled = true,  Port = 5001, WindowMode = WindowMode.Visible },
        ["web6"]  = new() { Enabled = false, Port = 5002, WindowMode = WindowMode.Visible },
        ["web7"]  = new() { Enabled = false, Port = 5003, WindowMode = WindowMode.Visible },
        ["web8"]  = new() { Enabled = false, Port = 5004, WindowMode = WindowMode.Visible },
        ["web9"]  = new() { Enabled = false, Port = 5005, WindowMode = WindowMode.Visible },
        ["web10"] = new() { Enabled = false, Port = 5006, WindowMode = WindowMode.Visible },
    };

    static Dictionary<string, string> DefaultGroups() => new()
    {
        ["all"]      = "web4,web5,web6,web7,web8,web9,web10",
        ["core"]     = "web4,web5",
        ["ai"]       = "web6",
        ["extended"] = "web7,web8,web9,web10",
    };
}
