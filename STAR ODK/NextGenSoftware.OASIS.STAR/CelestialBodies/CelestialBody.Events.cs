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
    public abstract partial class CelestialBody<T> where T : ICelestialBody, new()
    {
        private void SetMetaData()
        {
            CelestialBodyCore = (ICelestialBodyCore)Mapper.MapBaseHolonProperties(this, CelestialBodyCore);

            //CelestialHolon Properties
            CelestialBodyCore.MetaData["Age"] = this.Age;
            CelestialBodyCore.MetaData["Colour"] = ColorTranslator.ToHtml(this.Colour);
            CelestialBodyCore.MetaData["EclipticLatitute"] = this.EclipticLatitute;
            CelestialBodyCore.MetaData["EclipticLongitute"] = this.EclipticLongitute;
            CelestialBodyCore.MetaData["EquatorialLatitute"] = this.EquatorialLatitute;
            CelestialBodyCore.MetaData["EquatorialLongitute"] = this.EquatorialLongitute;
            CelestialBodyCore.MetaData["GalacticLatitute"] = this.GalacticLatitute;
            CelestialBodyCore.MetaData["GalacticLongitute"] = this.GalacticLongitute;
            CelestialBodyCore.MetaData["HorizontalLatitute"] = this.HorizontalLatitute;
            CelestialBodyCore.MetaData["HorizontalLongitute"] = this.HorizontalLongitute;
            CelestialBodyCore.MetaData["Radius"] = this.Radius;
            CelestialBodyCore.MetaData["Size"] = this.Size;
            CelestialBodyCore.MetaData["SpaceQuadrant"] = this.SpaceQuadrant;
            CelestialBodyCore.MetaData["SpaceSector"] = this.SpaceSector;
            CelestialBodyCore.MetaData["SuperGalacticLatitute"] = this.SuperGalacticLatitute;
            CelestialBodyCore.MetaData["SuperGalacticLongitute"] = this.SuperGalacticLongitute;
            CelestialBodyCore.MetaData["Temperature"] = this.Temperature;

            //CelestialBody Properties
            CelestialBodyCore.MetaData["CurrentOrbitAngleOfParentStar"] = this.CurrentOrbitAngleOfParentStar;
            CelestialBodyCore.MetaData["Density"] = this.Density;
            CelestialBodyCore.MetaData["DistanceFromParentStarInMetres"] = this.DistanceFromParentStarInMetres;
            CelestialBodyCore.MetaData["GravitaionalPull"] = this.GravitaionalPull;
            CelestialBodyCore.MetaData["Mass"] = this.Mass;
            CelestialBodyCore.MetaData["NumberActiveAvatars"] = this.NumberActiveAvatars;
            CelestialBodyCore.MetaData["NumberRegisteredAvatars"] = this.NumberRegisteredAvatars;
            CelestialBodyCore.MetaData["OrbitPositionFromParentStar"] = this.OrbitPositionFromParentStar;
            CelestialBodyCore.MetaData["RotationSpeed"] = this.RotationSpeed;
            CelestialBodyCore.MetaData["RotationPeriod"] = this.RotationPeriod;
            CelestialBodyCore.MetaData["TiltAngle"] = this.TiltAngle;
            CelestialBodyCore.MetaData["Weight"] = this.Weight;
        }

        private OASISResult<ICelestialBody> ProcessSaveResult(OASISResult<ICelestialBody> result, OASISResult<IZome> celestialBodyHolonResult)
        {
            OASISResultHelper.CopyResult(celestialBodyHolonResult, result);

            if (celestialBodyHolonResult != null && !celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
            {
                result.SavedCount++;
                //SetProperties(celestialBodyHolonResult.Result); //Redundant. TODO: Double check! ;-)
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} holon. Reason: {celestialBodyHolonResult.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                    result.IsSaved = true;
                }

                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Error, $"Error Creating CelestialBody {this.Name}. Reason: {result.Message}");
            }
            else
            {
                result.IsSaved = true;

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Success, $"CelestialBody {this.Name} Saved Successfully.");
            }

            IsSaving = false;
            OnCelestialBodySaved?.Invoke(this, new CelestialBodySavedEventArgs() { Result = result });
            return result;
        }

        private OASISResult<T> ProcessSaveResult<T>(OASISResult<T> result) where T: IHolon
        {
            //OASISResultHelper.CopyResult<T1, T2>(celestialBodyHolonResult, result);

            if (result != null && !result.IsError && result.Result != null)
            {
                result.SavedCount++;
                //SetProperties(celestialBodyHolonResult.Result); //Redundant. TODO: Double check! ;-)
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} holon. Reason: {result.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                    result.IsSaved = true;
                }

                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Error, $"Error Creating CelestialBody {this.Name}. Reason: {result.Message}");
            }
            else
            {
                result.IsSaved = true;

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Success, $"CelestialBody {this.Name} Created.");
            }

            IsSaving = false;
            OnCelestialBodySaved?.Invoke(this, new CelestialBodySavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            return result;
        }

        /*
        private async Task<bool> SaveZomesAndHolons()
        {
            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                //await zome.SaveHolonAsync(zome);
                await zome.SaveHolonAsync(this.RustHolonType, zome); //TODO: FIX ASAP!

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    await zome.SaveHolonAsync(this.RustHolonType, holon);
                }
            }

            return true;
        }
        */

        public IZome GetZomeThatHolonBelongsTo(IHolon holon)
        {
            return (IZome)CelestialBodyCore.AllChildren.FirstOrDefault(x => x.Id == holon.Id).ParentHolon;
        }

        public List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return CelestialBodyCore.AllChildren.Where(x => x.ParentHolon.Id == zome.Id).ToList();
        }

        public IZome GetZomeByName(string name)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Name == name);
        }

        public IZome GetZomeById(Guid id)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Id == id);
        }
        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeAdded(object sender, ZomeAddedEventArgs e)
        {
            OnZomeAdded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeRemoved(object sender, ZomeRemovedEventArgs e)
        {
            OnZomeRemoved?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void CelestialBodyCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);

            // TODO: Dont think this is needed now?
            // This was going to load each of the zomes holons once the zomes were loaded for this Planet. 
            // But maybe it is better to allow them be lazy loaded as and when they are needed rather than pulling them all back in one go?
            // Trade offs between the 2 approaches... for now we leave as lazy loading so will only load when they are needed...

            /*
            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }*/

            //TODO: Not sure whether to delegate holons being loaded by zomes if can just load direct from PlanetCore?
            //Nice for Zomes to manage their own collections of holons (good practice) so will see... :)
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

        private async void CelestialBodyCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES/HOLONS ARE AUTOMATICALLY SAVED WHEN CELESTIALBODY IS (IF SAVECHILDREN PARAM IS SET TO TRUE, OTHERWISE CAN CALL SAVEZOMES LATER TO SAVE ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO SAVE INDIVIDUAL ZOMES/HOLONS BY EITHER NAME/ID/PROVIDERKEY

            /*
            //TODO: Come back to this...
            return;

            //TODO: Handle error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Planet)
                {
                    // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                    this.ProviderUniqueStorageKey = e.Result.Result.ProviderUniqueStorageKey;

                    //Just in case the zomes/holons have been added since the planet was last saved.
                    foreach (Zome zome in CelestialBodyCore.Zomes)
                    {
                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        zome.ParentHolonId = this.Id;
                        zome.ParentHolon = this;

                        // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                        // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                        foreach (Holon holon in zome.Holons)
                        {
                            holon.ParentHolon = zome;
                            holon.ParentHolonId = zome.Id;

                            switch (HolonType)
                            {
                                case HolonType.Star:
                                    zome.ParentStar = (IStar)this;
                                    zome.ParentStarId = this.Id;
                                    break;

                                case HolonType.Planet:
                                    zome.ParentPlanet = (IPlanet)this;
                                    zome.ParentPlanetId = this.Id;
                                    break;

                                case HolonType.Moon:
                                    zome.ParentMoon = (IMoon)this;
                                    zome.ParentMoonId = this.Id;
                                    break;
                            }
                        }

                        await zome.SaveHolonsAsync(zome.Holons);
                    }
                }
            }*/
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
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


        //TODO: Come back to this, this is what is fired when each zome is loaded once the celestialbody is loaded but I think for now we will lazy load them later...
        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES ARE AUTOMATICALLY LOADED WHEN CELESTIALBODY IS (IF LOADZOMES PARAM IS SET TO TRUE0, OTHERWISE CAN CALL LOADZOMES LATER TO LOAD ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO LOAD INDIVIDUAL ZOMES BY EITHER NAME/ID/PROVIDERKEY

            /*
            bool holonFound = false;

            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Id == e.Holon.Id)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual zome loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OApp).

            //TODO: Now all zomes and holons belonging to a planet (OApp) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                //IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);
                IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Id == e.Holon.Parent.Id);

                if (zome == null)
                {
                    zome = new Zome(e.Holon.Parent.Id);
                    zome.Holons.Add(e.Holon);
                    CelestialBodyCore.Zomes.Add(zome);
                    //CelestialBodyCore.Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));
                }

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
            */
        }

        private void Zome_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }
    }
}
