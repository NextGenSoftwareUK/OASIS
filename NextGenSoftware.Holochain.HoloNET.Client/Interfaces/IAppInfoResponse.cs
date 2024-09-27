namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IAppInfoResponse
    {
        AppInfo data { get; set; }
        string type { get; set; }
    }
}