using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Events
{
    public class AvatarManagerErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }

    }

    public class OASISErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }

    public class CelestialHolonLoadedEventArgs : EventArgs
    {
        public OASISResult<ICelestialHolon> Result { get; set; }
    }

    public class CelestialHolonSavedEventArgs : EventArgs
    {
        public OASISResult<ICelestialHolon> Result { get; set; }
    }

    public class CelestialHolonErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public OASISResult<ICelestialHolon> Result { get; set; }
    }

    public class CelestialBodyLoadedEventArgs : EventArgs
    {
        public OASISResult<ICelestialBody> Result { get; set; }
    }

    public class CelestialBodiesLoadedEventArgs : EventArgs
    {
        public OASISResult<ICelestialSpace> Result { get; set; }
        public IEnumerable<ICelestialBody> CelestialBodies { get; set; }
    }

    public class CelestialBodySavedEventArgs : EventArgs
    {
        public OASISResult<ICelestialBody> Result { get; set; }
    }

    public class CelestialBodiesSavedEventArgs : EventArgs
    {
        public OASISResult<ICelestialSpace> Result { get; set; }
        public IEnumerable<ICelestialBody> CelestialBodies { get; set; }
    }

    public class CelestialBodyErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public OASISResult<ICelestialBody> Result { get; set; }
    }

    public class CelestialSpaceLoadedEventArgs : EventArgs
    {
        public OASISResult<ICelestialSpace> Result { get; set; }
    }

    public class CelestialSpaceSavedEventArgs : EventArgs
    {
        public OASISResult<ICelestialSpace> Result { get; set; }
    }

    public class CelestialSpaceErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public OASISResult<ICelestialSpace> Result { get; set; }
    }

    public class ZomesLoadedEventArgs : EventArgs
    {
        public List<IZome> Zomes { get; set; }
    }

    public class ZomeSavedEventArgs : EventArgs
    {
        public OASISResult<IZome> Result { get; set; }
    }

    public class HolonLoadedEventArgs : EventArgs
    {
        public OASISResult<IHolon> Result { get; set; }
    }

    public class HolonsLoadedEventArgs : EventArgs
    {
        public OASISResult<IEnumerable<IHolon>> Result { get; set; }
    }

    public class HolonSavedEventArgs : EventArgs
    {
        public OASISResult<IHolon> Result { get; set; }
    }
    public class HolonSavedEventArgs<T> : EventArgs
    {
        public OASISResult<T> Result { get; set; }
    }

    public class HolonsSavedEventArgs : EventArgs
    {
        public OASISResult<IEnumerable<IHolon>> Result { get; set; }
    }

    public class HolonsSavedEventArgs<T> : EventArgs
    {
        public OASISResult<IEnumerable<T>> Result { get; set; }
    }

    public class HolonAddedEventArgs : EventArgs
    {
        public OASISResult<IEnumerable<IHolon>> Result { get; set; }
    }

    public class HolonRemovedEventArgs : EventArgs
    {
        public OASISResult<IEnumerable<IHolon>> Result { get; set; }
    }

    public class ZomeErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
        //public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
    /*
    public class ConnectedEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
    }*/
}
