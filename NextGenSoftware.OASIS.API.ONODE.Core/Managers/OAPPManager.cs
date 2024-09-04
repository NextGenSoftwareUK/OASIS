using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPManager : OASISManager//, INFTManager
    {
       //private static OAPPManager _instance = null;

        public OAPPManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public OAPPManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListOAPPsCreatedByAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();
            string errorMessage = "Error occured in OAPPManager.ListOAPPsCreatedByAvatarAsync, Reason:";

            try
            {
                OASISResult<IEnumerable<OAPP>> oapps = await Data.LoadHolonsForParentAsync<OAPP>(avatarId, HolonType.OAPP, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (oapps != null && oapps.Result != null && !oapps.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPP>, IEnumerable<IOAPP>>(oapps);
                    result.Result = [.. oapps.Result];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonsForParentAsync, reason: {oapps.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOAPP>> ListOAPPsCreatedByAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();
            string errorMessage = "Error occured in OAPPManager.ListOAPPsCreatedByAvatar, Reason:";

            try
            {
                OASISResult<IEnumerable<OAPP>> oapps = Data.LoadHolonsForParent<OAPP>(avatarId, HolonType.OAPP, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (oapps != null && oapps.Result != null && !oapps.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPP>, IEnumerable<IOAPP>>(oapps);
                    result.Result = [.. oapps.Result];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonsForParent, reason: {oapps.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListOAPPsInstalledByAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();
            string errorMessage = "Error occured in OAPPManager.ListOAPPsInstalledByAvatarAsync, Reason:";

            //try
            //{
            //    OASISResult<IEnumerable<OAPP>> oapps = await Data.LoadHolonsForParentAsync<OAPP>(avatarId, HolonType.OAPP, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            //    if (oapps != null && oapps.Result != null && !oapps.IsError)
            //    {
            //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPP>, IEnumerable<IOAPP>>(oapps);
            //        result.Result = [.. oapps.Result];
            //    }
            //    else
            //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonsForParentAsync, reason: {oapps.Message}");
            //}
            //catch (Exception e)
            //{
            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            //}

            return result;
        }

        public OASISResult<IEnumerable<IOAPP>> ListOAPPsInstalledByAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return new OASISResult<IEnumerable<IOAPP>>();
        }

        public async Task<OASISResult<IEnumerable<IOAPP>>> ListAllOAPPsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();
            string errorMessage = "Error occured in OAPPManager.ListAllOAPPsAsync, Reason:";

            try
            {
                //result = DecodeNFTMetaData(await Data.LoadAllHolonsAsync(HolonType.NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                //OASISResult<IEnumerable<IHolon>> holons = await Data.LoadAllHolonsAsync(HolonType.OAPP, true, true, 0, true, false, HolonType.All, 0, providerType);
                OASISResult<IEnumerable<OAPP>> oapps = await Data.LoadAllHolonsAsync<OAPP>(HolonType.OAPP, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (oapps != null && oapps.Result != null && !oapps.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPP>, IEnumerable<IOAPP>>(oapps);
                    result.Result = [.. oapps.Result];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadAllHolonsAsync, reason: {oapps.Message}");

                //if (holons != null && holons.Result != null && !holons.IsError)
                //{
                //    List<IOAPP> oapps = new List<IOAPP>();

                //    foreach (IHolon holon in holons.Result) 
                //    {
                //        IOAPP OAPP = Mapper<IHolon, OAPP>.MapBaseHolonProperties(holon);
                //        OAPP.GenesisType = holon.MetaData[]
                //    }
                //}
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IOAPP>> ListAllOAPPs(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IOAPP>> result = new OASISResult<IEnumerable<IOAPP>>();
            string errorMessage = "Error occured in OAPPManager.ListAllOAPPs, Reason:";

            try
            {
                OASISResult<IEnumerable<OAPP>> oapps = Data.LoadAllHolons<OAPP>(HolonType.OAPP, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (oapps != null && oapps.Result != null && !oapps.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<OAPP>, IEnumerable<IOAPP>>(oapps);
                    result.Result = [.. oapps.Result];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadAllHolons, reason: {oapps.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            string errorMessage = "Error occured in OAPPManager.LoadOAPPAsync, Reason:";

            try
            {
                OASISResult<OAPP> oapp = await Data.LoadHolonAsync<OAPP>(OAPPId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (oapp != null && oapp.Result != null && !oapp.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<OAPP, IOAPP>(oapp);
                    result.Result = oapp.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolonAsync, reason: {oapp.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOAPP> LoadOAPP(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            string errorMessage = "Error occured in OAPPManager.LoadOAPP, Reason:";

            try
            {
                OASISResult<OAPP> oapp = Data.LoadHolon<OAPP>(OAPPId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (oapp != null && oapp.Result != null && !oapp.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<OAPP, IOAPP>(oapp);
                    result.Result = oapp.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Data.LoadHolon, reason: {oapp.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOAPP>> CreateOAPPAsync(OAPPType OAPPType, GenesisType genesisType, Guid avatarId, ICelestialBody celestialBody = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            string errorMessage = "Error occured in OAPPManager.CreateOAPPAsync, Reason:";

            OAPP OAPP = new OAPP()
            {
                CelestialBody = celestialBody, //The CelestialBody that represents the OAPP (if any).
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType
            };

            OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(OAPP, avatarId, true, true, 0, true, false, providerType);

            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
            {
                if (celestialBody != null)
                {
                    celestialBody.MetaData["OAPPID"] = saveHolonResult.Result.Id;
                    //celestialBody.MetaData["OAPP"] = JsonSerializer.Serialize(saveHolonResult.Result); //TODO: Don't think we need to store the whole OAPP here (especially since it extends Holon so has a LOT of props) and is duplicated when the OAPP can be easily loaded from the ID.

                    OASISResult<ICelestialBody> celestialBodyResult = await celestialBody.SaveAsync(true, true, 0, true, false, providerType);

                    if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
                        result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OAPP to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                }
                else
                    result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OAPP to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");

            return result;
        }

        public OASISResult<IOAPP> CreateOAPP(OAPPType OAPPType, GenesisType genesisType, Guid avatarId, ICelestialBody celestialBody = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            string errorMessage = "Error occured in OAPPManager.CreateOAPP, Reason:";

            OAPP OAPP = new OAPP()
            {
                CelestialBody = celestialBody, //The CelestialBody that represents the OAPP (if any).
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType
            };

            OASISResult<IHolon> saveHolonResult = Data.SaveHolon(OAPP, avatarId, true, true, 0, true, false, providerType);

            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
            {
                if (celestialBody != null)
                {
                    celestialBody.MetaData["OAPPID"] = saveHolonResult.Result.Id;
                    //celestialBody.MetaData["OAPP"] = JsonSerializer.Serialize(saveHolonResult.Result); //TODO: Don't think we need to store the whole OAPP here (especially since it extends Holon so has a LOT of props) and is duplicated when the OAPP can be easily loaded from the ID.

                    OASISResult<ICelestialBody> celestialBodyResult = celestialBody.Save(true, true, 0, true, false, providerType);

                    if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
                        result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OAPP to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");
                }
                else
                    result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OAPP to the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveHolonResult.Message}");

            return result;
        }


        public async Task<OASISResult<IOAPP>> SaveOAPPAsync(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IHolon> saveResult = await OAPP.SaveAsync();

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IOAPP>(saveResult);
                result.Result = (IOAPP)saveResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured in OAPPManager.SaveOAPPAsync saving the OAPP. Reason: {saveResult.Message}");

            return result;
        }

        public OASISResult<IOAPP> SaveOAPP(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IHolon> saveResult = OAPP.Save();

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IOAPP>(saveResult);
                result.Result = (IOAPP)saveResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured in OAPPManager.SaveOAPP saving the OAPP. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            OAPP.IsPublished = true;
            result = await SaveOAPPAsync(OAPP);

            return result;
        }

        public OASISResult<IOAPP> PublishOAPP(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            OAPP.IsPublished = true;
            result = SaveOAPP(OAPP);

            return result;
        }

        public async Task<OASISResult<IOAPP>> PublishOAPPAsync(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IOAPP> loadResult = await LoadOAPPAsync(OAPPId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await PublishOAPPAsync(loadResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in PublishOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPP> PublishOAPP(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IOAPP> loadResult = LoadOAPP(OAPPId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = PublishOAPP(loadResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in PublishOAPP loading the OAPP with the LoadOAPP method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPP>> UnPublishOAPPAsync(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            OAPP.IsPublished = false;
            result = await SaveOAPPAsync(OAPP);

            return result;
        }

        public OASISResult<IOAPP> UnPublishOAPP(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            OAPP.IsPublished = false;
            result = SaveOAPP(OAPP);

            return result;
        }

        public async Task<OASISResult<IOAPP>> UnPublishOAPPAsync(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IOAPP> loadResult = await LoadOAPPAsync(OAPPId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnPublishOAPPAsync(loadResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPP> UnPublishOAPP(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IOAPP> loadResult = LoadOAPP(OAPPId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnPublishOAPP(loadResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPP loading the OAPP with the LoadOAPP method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPP>> InstallOAPPAsync(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            return result;
        }

        public async Task<OASISResult<IOAPP>> InstallOAPPAsync(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            return result;
        }

        public OASISResult<IOAPP> InstallOAPP(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();


            return result;
        }

        public OASISResult<IOAPP> InstallOAPP(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();


            return result;
        }

        public async Task<OASISResult<IOAPP>> UnInstallOAPPAsync(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            return result;
        }

        public async Task<OASISResult<IOAPP>> UnInstallOAPPAsync(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();

            return result;
        }

        public OASISResult<IOAPP> UnInstallOAPP(IOAPP OAPP)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();


            return result;
        }

        public OASISResult<IOAPP> UnInstallOAPP(Guid OAPPId)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();


            return result;
        }
    }
}