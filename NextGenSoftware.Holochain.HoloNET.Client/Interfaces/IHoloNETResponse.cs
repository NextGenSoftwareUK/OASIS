namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETResponse
    {
        byte[] data { get; set; }
        HoloNETResponseType HoloNETResponseType { get; set; }
        ulong id { get; set; }
        bool IsError { get; set; }
        string type { get; set; }
    }
}