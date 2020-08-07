using System;

namespace NextGenSoftware.Holochain.HDK.HoloNET.Client.Core
{
    public interface IHoloNETClientNET
    {
        //async Task<bool> Connect(Uri EndPoint);
        bool Connect(Uri EndPoint);
        bool Disconnect();
        bool SendData(string Data);
        string ReceiveData();

        NetSocketState NetSocketState { get; set; }
    }
}
