namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface ISignalData
    {
        byte[][] CellData { get; set; }
        byte[] Data { get; set; }
    }
}