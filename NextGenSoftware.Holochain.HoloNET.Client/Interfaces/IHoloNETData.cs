namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETData
    {
        dynamic data { get; set; }
        string type { get; set; }
    }
}