
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class StorageInfoResponse : IStorageInfoResponse
    {
        [Key("blobs")]
        public DnaStorageBlob[] blobs { get; set; }
    }
}