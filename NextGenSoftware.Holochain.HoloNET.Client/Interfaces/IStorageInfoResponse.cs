namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IStorageInfoResponse
    {
        DnaStorageBlob[] blobs { get; set; }
    }
}