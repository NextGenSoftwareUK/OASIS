using System;

namespace NextGenSoftware.OASIS.API.Core.Events
{
    public class Events
    {
        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        //public event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
       // public event HolonLoaded OnHolonLoaded;

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        //public event HolonsLoaded OnHolonsLoaded;

        public delegate void Initialized(object sender, EventArgs e);
       // public event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
       // public event ZomeError OnZomeError;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
       // public event ZomesLoaded OnZomesLoaded;
        

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;

    }
}
