using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using System.Drawing;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBody<T> : CelestialHolon, ICelestialBody where T : ICelestialBody, new()
    {
        public ICelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the star/planet/moon/etc (OApp), which links to all the other stars/planets/moons/etc/zomes/holons...
                                                                  
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonError OnHolonError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;

        //[BsonIgnore]
        public new Guid Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;

                if (CelestialBodyCore != null)
                    CelestialBodyCore.Id = value;
            }
        }

        //[BsonIgnore]
        public new Dictionary<ProviderType, string> ProviderUniqueStorageKey
        {
            get
            {
                return base.ProviderUniqueStorageKey;
            }
            set
            {
                base.ProviderUniqueStorageKey = value;

                if (CelestialBodyCore != null)
                    CelestialBodyCore.ProviderUniqueStorageKey = value;
            }
        }

        public override ReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                if (CelestialBodyCore != null)
                    return (ReadOnlyCollection<IHolon>)CelestialBodyCore.AllChildren;
                else
                    return Children.AsReadOnly();
            }
        }

        public long Mass { get; set; }
        public long Density { get; set; }
        public long RotationPeriod { get; set; } //How long it takes to rotate on its axis.
        public long OrbitPeriod { get; set; } //How long it takes to orbit its ParentStar.
        public long Weight { get; set; }
        public long GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        //public int OrbitPositionFromParentSuperStar { get; set; } //Only applies to SolarSystems. //TODO: Maybe better to make SolarSystem.ParentStar point to the SuperStar it orbits rather than the Star at the centre of it?
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public long DistanceFromParentStarInMetres { get; set; }
        public long RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NumberActiveAvatars { get; set; }

        public CelestialBody(Guid id, HolonType holonType, bool autoLoad = true) : base(id, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialBody(string providerKey, ProviderType providerType, HolonType holonType, bool autoLoad = true) : base(providerKey, providerType, holonType)
        {
            Initialize(autoLoad);
        }

        //public CelestialBody(Dictionary<ProviderType, string> providerKeys, HolonType holonType, bool autoLoad = true) : base(providerKeys, holonType)
        //{
        //    Initialize(autoLoad);
        //}

        public CelestialBody(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        ////TODO: Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic LoadAsync<T> version instead! 
        //[Obsolete("Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic LoadAsync<T> version instead!")]
        //public new async Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(await CelestialBodyCore.LoadAsync(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));
        //    //OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(await CelestialBodyCore.GlobalHolonData.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));

        //    if (result != null && result.IsError)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.LoadAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    }
        //    else
        //        OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = result });

        //    return result;
        //}

        ////TODO: Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic Load<T> version instead! 
        //[Obsolete("Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic Load<T> version instead!")]
        //public new OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(CelestialBodyCore.Load(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));

        //    if (result != null && result.IsError)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.Load method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    }
        //    else
        //        OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = result });

        //    return result;
        //}

    }
}
