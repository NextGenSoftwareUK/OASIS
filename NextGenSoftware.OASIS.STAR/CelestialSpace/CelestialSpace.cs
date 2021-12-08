using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        public List<ICelestialBody> CelestialBodies = new List<ICelestialBody>();

        public CelestialSpace(HolonType holonType) : base(holonType)
        {
            Initialize();  
        }

        public CelestialSpace(Guid id, HolonType holonType) : base(id, holonType)
        {
            Initialize();
        }

        public CelestialSpace(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        {
            Initialize();  
        }

        protected override void Initialize()
        {
            //TODO: Load and Wireup Events like CelestialBody, etc.
            base.Initialize();
        }

        protected override async Task InitializeAsync()
        {
            //TODO: Load and Wireup Events like CelestialBody, etc.
            await base.InitializeAsync();
        }

        public async Task<OASISResult<ICelestialSpace>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<ICelestialBody> celestialBodyResult = null;

            //Save all CelestialBodies contained within this space.
            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                celestialBodyResult = await celestialBody.SaveAsync(saveChildren, continueOnError);

                if (!(celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError))
                {
                    result.ErrorCount++;
                    string message = $"There was an error whilst saving the CelestialBody {celestialBody.Name} of type {Enum.GetName(typeof(HolonType), celestialBody.HolonType)}. Reason: {celestialBodyResult.Message}";
                    result.InnerMessages.Add(message);
                    ErrorHandling.HandleWarning(ref celestialBodyResult, message);

                    if (!continueOnError)
                        break;
                }
                else
                    result.SavedCount++;
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured saving {CelestialBodies.Count} CelestialBodies in the CelestialSpace {this.Name} of type {Enum.GetName(typeof(HolonType), this.HolonType)}. Please check the logs and InnerMessages for more info.";
                
                if (result.SavedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }
            }
            else
                result.IsSaved = true;

            //base.OnCelestialHolonSaved?.Invoke(this, new System.EventArgs());
            return result;
        }

        public OASISResult<ICelestialSpace> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            return SaveAsync<T>(saveChildren, continueOnError).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadAsync()
        {
            //TODO: Implement ASAP.
            return new OASISResult<IHolon>();
            //return await CelestialBodyCore.LoadCelestialBodyAsync();
        }

        public override OASISResult<IHolon> Load()
        {
            //TODO: Implement ASAP.
            return new OASISResult<IHolon>();
            // return CelestialBodyCore.LoadCelestialBody();
        }

        protected void RegisterCelestialBodies(IEnumerable<ICelestialBody> celestialBodies)
        {
            this.CelestialBodies.AddRange(celestialBodies);
        }

        protected void UnregisterAllCelestialBodies()
        {
            this.CelestialBodies = new List<ICelestialBody>(); 
        }
    }
}