using System;

namespace NextGenSoftware.OASIS.API.Core.Events
{
    public class Events
    {
        public delegate void Initialized(object sender, EventArgs e);
        public delegate void CelestialHolonLoaded(object sender, CelestialHolonLoadedEventArgs e);
        public delegate void CelestialHolonSaved(object sender, CelestialHolonSavedEventArgs e);
        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public delegate void HolonSaved<T>(object sender, HolonSavedEventArgs<T> e);
        public delegate void HolonsSaved(object sender, HolonsSavedEventArgs e);
        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public delegate void ZomeSaved(object sender, ZomeSavedEventArgs e);
        public delegate void HolonAdded(object sender, HolonAddedEventArgs e);
        public delegate void HolonRemoved(object sender, HolonRemovedEventArgs e);
        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public delegate void CelestialHolonError(object sender, CelestialHolonErrorEventArgs e);

        // public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        // public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;
    }
}
