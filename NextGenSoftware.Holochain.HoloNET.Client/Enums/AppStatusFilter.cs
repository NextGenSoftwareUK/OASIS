

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum AppStatusFilter //May need to convert to lowecase string for each value such as enabled, disabled, etc in HoloNETClient when setting params in HoloNETAdminListAppsRequest etc.
    {
        Enabled,
        Disabled,
        Running,
        Stopped,
        Paused,
        All
    }
}


/*
export enum AppStatusFilter {
  Enabled = "enabled",
  Disabled = "disabled",
  Running = "running",
  Stopped = "stopped",
  Paused = "paused",
}
*/