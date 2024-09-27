namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IAppResponse
    {
        dynamic data { get; set; }
        string type { get; set; }
    }
}