using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{    public abstract partial class CelestialSpace
    {
        private void SetCelestialHolonMetaData()
        {
            //CelestialHolon Properties
            MetaData["Age"] = this.Age;
            MetaData["Colour"] = this.Colour;
            MetaData["EclipticLatitute"] = this.EclipticLatitute;
            MetaData["EclipticLongitute"] = this.EclipticLongitute;
            MetaData["EquatorialLatitute"] = this.EquatorialLatitute;
            MetaData["EquatorialLongitute"] = this.EquatorialLongitute;
            MetaData["GalacticLatitute"] = this.GalacticLatitute;
            MetaData["GalacticLongitute"] = this.GalacticLongitute;
            MetaData["HorizontalLatitute"] = this.HorizontalLatitute;
            MetaData["HorizontalLongitute"] = this.HorizontalLongitute;
            MetaData["Radius"] = this.Radius;
            MetaData["Size"] = this.Size;
            MetaData["SpaceQuadrant"] = this.SpaceQuadrant;
            MetaData["SpaceSector"] = this.SpaceSector;
            MetaData["SuperGalacticLatitute"] = this.SuperGalacticLatitute;
            MetaData["SuperGalacticLongitute"] = this.SuperGalacticLongitute;
            MetaData["Temperature"] = this.Temperature;
        }

        private void CelestialSpace_OnCelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e)
        {
            OnCelestialSpaceLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e)
        {
            OnCelestialSpaceSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e)
        {
            OnCelestialSpaceError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e)
        {
            OnCelestialSpacesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e)
        {
            OnCelestialSpacesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e)
        {
            OnCelestialSpacesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e)
        {
            OnCelestialBodiesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e)
        {
            OnCelestialBodiesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e)
        {
            OnCelestialBodiesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }
    }
}