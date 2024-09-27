namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETRequest
    {
        byte[] data { get; set; }
        ulong id { get; set; }
        string type { get; set; }
    }
}