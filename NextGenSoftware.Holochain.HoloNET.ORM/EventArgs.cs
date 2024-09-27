using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class HoloNETCollectionSavedResult : CallBackBaseEventArgs
    {
        public List<IHoloNETEntryBase> EntiesSaved { get; set; } = new List<IHoloNETEntryBase>();
        public List<IHoloNETEntryBase> EntiesAdded { get; set; } = new List<IHoloNETEntryBase>();
        public List<IHoloNETEntryBase> EntiesRemoved { get; set; } = new List<IHoloNETEntryBase>();
        public List<IHoloNETEntryBase> EntiesSaveErrors { get; set; } = new List<IHoloNETEntryBase>();
        public List<ZomeFunctionCallBackEventArgs> ConductorResponses { get; set; } = new List<ZomeFunctionCallBackEventArgs>();
        public List<string> ErrorMessages = new List<string>();
    }

    public class HoloNETCollectionLoadedResult<T> : CallBackBaseEventArgs where T : HoloNETEntryBase
    {
        public List<T> EntriesLoaded {  get; set; }
        public ZomeFunctionCallBackEventArgs ZomeFunctionCallBackEventArgs { get; set; }
    }
}