using NextGenSoftware.OASIS.Common;
using System;

namespace NextGenSoftware.OASIS.API.Core.Events
{
    public class EventDelegates
    {
        public delegate void Initialized(object sender, EventArgs e);
        //public delegate void CelestialHolonLoaded(object sender, CelestialHolonLoadedEventArgs e);
        //public delegate void CelestialHolonSaved(object sender, CelestialHolonSavedEventArgs e);
        //public delegate void CelestialHolonError(object sender, CelestialHolonErrorEventArgs e);
        public delegate void CelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e);
        public delegate void CelestialBodySaved(object sender, CelestialBodySavedEventArgs e);
        public delegate void CelestialBodyAdded(object sender, CelestialBodyAddedEventArgs e);
        public delegate void CelestialBodyRemoved(object sender, CelestialBodyRemovedEventArgs e);
        public delegate void CelestialBodyError(object sender, CelestialBodyErrorEventArgs e);
        public delegate void CelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e);
        public delegate void CelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e);
        public delegate void CelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e);
        public delegate void CelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e);
        public delegate void CelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e);
        public delegate void CelestialSpaceAdded(object sender, CelestialSpaceAddedEventArgs e);
        public delegate void CelestialSpaceRemoved(object sender, CelestialSpaceRemovedEventArgs e);
        public delegate void CelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e);
        public delegate void CelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e);
        public delegate void CelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e);
        public delegate void CelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e);
        public delegate void ZomeLoaded(object sender, ZomeLoadedEventArgs e);
        //public delegate void ZomeSaved<T>(object sender, ZomeSavedEventArgs<T> e);
        public delegate void ZomeSaved(object sender, ZomeSavedEventArgs e);
        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public delegate void ZomeAdded(object sender, ZomeAddedEventArgs e);
        public delegate void ZomeRemoved(object sender, ZomeRemovedEventArgs e);
        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public delegate void ZomesSaved(object sender, ZomesSavedEventArgs e);
        public delegate void ZomesError(object sender, ZomesErrorEventArgs e);
        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public delegate void HolonLoadedGeneric<T>(object sender, HolonLoadedEventArgs<T> e);
        // public delegate void HolonSaved<T>(object sender, HolonSavedEventArgs<T> e);
        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public delegate void HolonError(object sender, HolonErrorEventArgs e);
        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public delegate void HolonsSaved(object sender, HolonsSavedEventArgs e);
        public delegate void HolonsError(object sender, HolonsErrorEventArgs e);
        public delegate void HolonAdded(object sender, HolonAddedEventArgs e);
        //public delegate void HolonRemoved<T>(object sender, HolonRemovedEventArgs<T> e);
        public delegate void HolonRemoved(object sender, HolonRemovedEventArgs e);
        public delegate void HolonDeleted(object sender, HolonDeletedEventArgs e);
        public delegate void OASISManagerError(object sender, OASISErrorEventArgs e);
        public delegate void StorageProviderError(object sender, OASISErrorEventArgs e);

        // public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        // public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;
    }
}
