
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class SignalData : ISignalData
    {
        [Key(0)]
        public byte[][] CellData { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }
}