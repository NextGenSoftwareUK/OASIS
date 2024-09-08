using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPManager : OASISManager
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

        public async Task<OASISResult<IOAPPDNA>> CreateOAPPAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, Guid avatarId, ICelestialBody celestialBody = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.CreateOAPPAsync, Reason:";

            OAPP OAPP = new OAPP()
            {
                Id = Guid.NewGuid(),
                Name = OAPPName,
                Description = OAPPDescription,
                CelestialBody = celestialBody, //The CelestialBody that represents the OAPP (if any).
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType
            };

            OAPPDNA OAPPDNA = new OAPPDNA()
            {
                OAPPId = OAPP.Id,
                OAPPName = OAPPName,
                Description = OAPPDescription,
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType,
                CreatedByAvtarId = avatarId,
                CreatedDate = DateTime.Now,
                Version = "1.0.0"
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
                    {
                        result.Result = OAPPDNA;
                        result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
                    }
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

        public OASISResult<IOAPPDNA> CreateOAPP(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, Guid avatarId, ICelestialBody celestialBody = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.CreateOAPP, Reason:";

            OAPP OAPP = new OAPP()
            {
                Id = Guid.NewGuid(),
                Name = OAPPName,
                Description = OAPPDescription,
                CelestialBody = celestialBody, //The CelestialBody that represents the OAPP (if any).
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType
            };

            OAPPDNA OAPPDNA = new OAPPDNA()
            {
                OAPPId = OAPP.Id,
                OAPPName = OAPPName,
                Description = OAPPDescription,
                CelestialBodyId = celestialBody != null ? celestialBody.Id : Guid.Empty,
                OAPPType = OAPPType,
                GenesisType = genesisType,
                CreatedByAvtarId = avatarId,
                CreatedDate = DateTime.Now,
                Version = "1.0.0"
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
                    {
                        result.Result = OAPPDNA;
                        result.Message = $"Successfully created the OAPP on the {Enum.GetName(typeof(ProviderType), providerType)} provider by AvatarId {avatarId} for OAPPType {Enum.GetName(typeof(OAPPType), OAPPType)} and GenesisType {Enum.GetName(typeof(GenesisType), genesisType)}.";
                    }
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


        public async Task<OASISResult<IOAPP>> SaveOAPPAsync(IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IHolon> saveResult = await OAPP.SaveAsync(true, true, 0, true, false, providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IOAPP>(saveResult);
                result.Result = (IOAPP)saveResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured in OAPPManager.SaveOAPPAsync saving the OAPP. Reason: {saveResult.Message}");

            return result;
        }

        public OASISResult<IOAPP> SaveOAPP(IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            OASISResult<IHolon> saveResult = OAPP.Save(true, true, 0, true, false, providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, IOAPP>(saveResult);
                result.Result = (IOAPP)saveResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured in OAPPManager.SaveOAPP saving the OAPP. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> PublishOAPPAsync(IOAPPDNA OAPPDNA, string fullPathToOAPP, Guid avatarId, bool registerOnSTARNET = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.PublishOAPPAsync. Reason: ";

            try
            {
                await File.WriteAllTextAsync(Path.Combine(fullPathToOAPP, "OASISDNA.json"), JsonSerializer.Serialize((OAPPDNA)OAPPDNA));
                string publishedOAPPPath = string.Concat(fullPathToOAPP, "\\Published\\", OAPPDNA.OAPPName, ".oapp");
                //Directory.CreateDirectory(publichedOAPPPath);
                ZipFile.CreateFromDirectory(fullPathToOAPP, publishedOAPPPath);

                OASISResult<IOAPP> OAPPResult = await LoadOAPPAsync(OAPPDNA.OAPPId);

                if (OAPPResult != null && OAPPResult.Result != null && !OAPPResult.IsError)
                {
                    if (registerOnSTARNET)
                        OAPPResult.Result.PublishedOAPP = await File.ReadAllBytesAsync(publishedOAPPPath);

                    OAPPResult.Result.PublishedOn = DateTime.Now;
                    OAPPResult.Result.PublishedByAvatarId = avatarId;
                    OAPPDNA.PublishedDate = OAPPResult.Result.PublishedOn;
                    OAPPDNA.PublishedByAvatarId = avatarId;

                    OASISResult<IOAPP> OAPPSaveResult = await SaveOAPPAsync(OAPPResult.Result, providerType);
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IOAPP, IOAPPDNA>(OAPPSaveResult);
                }
            }
            catch (Exception ex) 
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPDNA> PublishOAPP(IOAPPDNA OAPPDNA, string fullPathToOAPP, Guid avatarId, bool registerOnSTARNET = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.PublishOAPP. Reason: ";

            try
            {
                File.WriteAllText(Path.Combine(fullPathToOAPP, "OASISDNA.json"), JsonSerializer.Serialize((OAPPDNA)OAPPDNA));
                string publishedOAPPPath = string.Concat(fullPathToOAPP, "\\Published\\", OAPPDNA.OAPPName, ".oapp");
                //Directory.CreateDirectory(publichedOAPPPath);
                ZipFile.CreateFromDirectory(fullPathToOAPP, publishedOAPPPath);

                OASISResult<IOAPP> OAPPResult = LoadOAPP(OAPPDNA.OAPPId);

                if (OAPPResult != null && OAPPResult.Result != null && !OAPPResult.IsError)
                {
                    if (registerOnSTARNET)
                        OAPPResult.Result.PublishedOAPP = File.ReadAllBytes(publishedOAPPPath);

                    OAPPResult.Result.PublishedOn = DateTime.Now;
                    OAPPResult.Result.PublishedByAvatarId = avatarId;
                    OAPPDNA.PublishedDate = OAPPResult.Result.PublishedOn;
                    OAPPDNA.PublishedByAvatarId = avatarId;

                    OASISResult<IOAPP> OAPPSaveResult = SaveOAPP(OAPPResult.Result, providerType);
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IOAPP, IOAPPDNA>(OAPPSaveResult);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> PublishOAPPAsync(Guid OAPPId, string fullPathToOAPP, Guid avatarId, bool registerOnSTARNET = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> loadResult = await LoadOAPPAsync(OAPPId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await PublishOAPPAsync(ConvertOAPPToOAPPDNA(loadResult.Result), fullPathToOAPP, avatarId, registerOnSTARNET, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in PublishOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPDNA> PublishOAPP(Guid OAPPId, string fullPathToOAPP, Guid avatarId, bool registerOnSTARNET = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> loadResult = LoadOAPP(OAPPId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = PublishOAPP(ConvertOAPPToOAPPDNA(loadResult.Result), fullPathToOAPP, avatarId, registerOnSTARNET, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in PublishOAPP loading the OAPP with the LoadOAPP method, reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> UnPublishOAPPAsync(IOAPPDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>(OAPPDNA);
            OASISResult<IOAPP> oappResult = await LoadOAPPAsync(OAPPDNA.OAPPId, providerType);
            string errorMessage = "Error occured in UnPublishOAPPAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                oappResult.Result.PublishedOn = DateTime.MinValue;
                oappResult.Result.PublishedByAvatarId = Guid.Empty;

                oappResult = await SaveOAPPAsync(oappResult.Result, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    OAPPDNA.PublishedDate = DateTime.MinValue;
                    OAPPDNA.PublishedByAvatarId = Guid.Empty;
                    result.Message = "OAPP Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP with the SaveOAPPAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP with the LoadOAPPAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<IOAPPDNA> UnPublishOAPP(IOAPPDNA OAPPDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>(OAPPDNA);
            OASISResult<IOAPP> oappResult = LoadOAPP(OAPPDNA.OAPPId, providerType);
            string errorMessage = "Error occured in UnPublishOAPP. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                oappResult.Result.PublishedOn = DateTime.MinValue;
                oappResult.Result.PublishedByAvatarId = Guid.Empty;

                oappResult = SaveOAPP(oappResult.Result, providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    OAPPDNA.PublishedDate = DateTime.MinValue;
                    OAPPDNA.PublishedByAvatarId = Guid.Empty;
                    result.Message = "OAPP Unpublised";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP with the SaveOAPP method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the OAPP with the LoadOAPP method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> UnPublishOAPPAsync(IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in UnPublishOAPPAsync. Reason: ";

            OAPP.PublishedOn = DateTime.MinValue;
            OAPP.PublishedByAvatarId = Guid.Empty;

            OASISResult<IOAPP> oappResult = await SaveOAPPAsync(OAPP, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = ConvertOAPPToOAPPDNA(OAPP);
                result.Message = "OAPP Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP with the SaveOAPPAsync method, reason: {oappResult.Message}");
            
            return result;
        }

        public OASISResult<IOAPPDNA> UnPublishOAPP(IOAPP OAPP, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in UnPublishOAPP. Reason: ";

            OAPP.PublishedOn = DateTime.MinValue;
            OAPP.PublishedByAvatarId = Guid.Empty;

            OASISResult<IOAPP> oappResult = SaveOAPP(OAPP, providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = ConvertOAPPToOAPPDNA(OAPP);
                result.Message = "OAPP Unpublised";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the OAPP with the SaveOAPP method, reason: {oappResult.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> UnPublishOAPPAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> loadResult = await LoadOAPPAsync(OAPPId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnPublishOAPPAsync(loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IOAPPDNA> UnPublishOAPP(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> loadResult = LoadOAPP(OAPPId, providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = UnPublishOAPP(loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnPublishOAPP loading the OAPP with the LoadOAPP method, reason: {loadResult.Message}");

            return result;
        }

        //public async Task<OASISResult<IOAPP>> InstallOAPPAsync(IOAPP OAPP, string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPP> result = new OASISResult<IOAPP>();
        //    string errorMessage = "Error occured in OAPPManager.InstallOAPPAsync. Reason: ";

        //    try
        //    {
        //        ZipFile.ExtractToDirectory(fullPathToPublishedOAPPFile, fullInstallPath, Encoding.Default, true);

        //        InstalledOAPP installedOAPP = new InstalledOAPP() { OAPP = OAPP };
        //        installedOAPP.MetaData["OAPPID"] = OAPP.Id;

        //        OASISResult<IHolon> saveResult = await installedOAPP.SaveAsync();

        //        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //        {
        //            result.Message = "OAPP Installed";
        //            result.IsSaved = true;
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
        //    }
        //    catch (Exception ex) 
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IOAPP> InstallOAPP(IOAPP OAPP, string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPP> result = new OASISResult<IOAPP>();
        //    string errorMessage = "Error occured in OAPPManager.InstallOAPP. Reason: ";

        //    try
        //    {
        //        ZipFile.ExtractToDirectory(fullPathToPublishedOAPPFile, fullInstallPath, Encoding.Default, true);

        //        InstalledOAPP installedOAPP = new InstalledOAPP() { OAPP = OAPP };
        //        OASISResult<IHolon> saveResult = installedOAPP.Save();

        //        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //        {
        //            result.Message = "OAPP Installed";
        //            result.IsSaved = true;
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save method. Reason: {saveResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IOAPP>> InstallOAPPAsync(Guid OAPPId, string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPP> result = new OASISResult<IOAPP>();
        //    result = await LoadOAPPAsync(OAPPId, providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //        result = await InstallOAPPAsync(result.Result, fullPathToPublishedOAPPFile, fullInstallPath, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPManager.InstallOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {result.Message}");

        //    return result;
        //}

        //public OASISResult<IOAPP> InstallOAPP(Guid OAPPId, string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IOAPP> result = new OASISResult<IOAPP>();
        //    result = LoadOAPP(OAPPId, providerType);

        //    if (result != null && !result.IsError && result.Result != null)
        //        result = InstallOAPP(result.Result, fullPathToPublishedOAPPFile, fullInstallPath, providerType);
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPManager.InstallOAPP loading the OAPP with the LoadOAPP method, reason: {result.Message}");

        //    return result;
        //}

        public async Task<OASISResult<IOAPPDNA>> InstallOAPPAsync(string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.InstallOAPPAsync. Reason: ";

            try
            {
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPFile, fullInstallPath, Encoding.Default, true);
                string OAPPDNAJson = await File.ReadAllTextAsync(Path.Combine(fullPathToPublishedOAPPFile, "OAPPDNA.json"));
                OAPPDNA OAPPDNA = JsonSerializer.Deserialize<OAPPDNA>(OAPPDNAJson);

                if (OAPPDNA != null)
                {
                    InstalledOAPP installedOAPP = new InstalledOAPP() { OAPPDNA = OAPPDNA };
                    OASISResult<IHolon> saveResult = await installedOAPP.SaveAsync();

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Message = "OAPP Installed";
                        result.Result = OAPPDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveAsync method. Reason: {saveResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPDNA> InstallOAPP(string fullPathToPublishedOAPPFile, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.InstallOAPP. Reason: ";

            try
            {
                ZipFile.ExtractToDirectory(fullPathToPublishedOAPPFile, fullInstallPath, Encoding.Default, true);
                string OAPPDNAJson = File.ReadAllText(Path.Combine(fullPathToPublishedOAPPFile, "OAPPDNA.json"));
                OAPPDNA OAPPDNA = JsonSerializer.Deserialize<OAPPDNA>(OAPPDNAJson);

                if (OAPPDNA != null)
                {
                    InstalledOAPP installedOAPP = new InstalledOAPP() { OAPPDNA = OAPPDNA };
                    OASISResult<IHolon> saveResult = installedOAPP.Save();

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Message = "OAPP Installed";
                        result.Result = OAPPDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Save method. Reason: {saveResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> InstallOAPPAsync(IOAPP OAPP, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.InstallOAPPAsync. Reason: ";

            try
            {
                string OAPPPath = Path.Combine("temp", OAPP.Name, ".oapp");
                await File.WriteAllBytesAsync(OAPPPath, OAPP.PublishedOAPP);
                result = await InstallOAPPAsync(OAPPPath, fullInstallPath, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public OASISResult<IOAPPDNA> InstallOAPP(IOAPP OAPP, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            string errorMessage = "Error occured in OAPPManager.InstallOAPP. Reason: ";

            try
            {
                string OAPPPath = Path.Combine("temp", OAPP.Name, ".oapp");
                File.WriteAllBytes(OAPPPath, OAPP.PublishedOAPP);
                result = InstallOAPP(OAPPPath, fullInstallPath, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> InstallOAPPAsync(Guid OAPPId, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> OAPPResult = await LoadOAPPAsync(OAPPId, providerType);

            if (OAPPResult != null && !OAPPResult.IsError && OAPPResult.Result != null)
                result = await InstallOAPPAsync(OAPPResult.Result, fullInstallPath, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPManager.InstallOAPPAsync loading the OAPP with the LoadOAPPAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<IOAPPDNA> InstallOAPP(Guid OAPPId, string fullInstallPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IOAPP> OAPPResult = LoadOAPP(OAPPId, providerType);

            if (OAPPResult != null && !OAPPResult.IsError && OAPPResult.Result != null)
                result = InstallOAPP(OAPPResult.Result, fullInstallPath, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPPManager.InstallOAPP loading the OAPP with the LoadOAPP method, reason: {result.Message}");

            return result;
        }

        public async Task<OASISResult<IOAPPDNA>> UnInstallOAPPAsync(IOAPPDNA OAPP, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return await UnInstallOAPPAsync(OAPP.OAPPId, avatarId, providerType);
        }

        public OASISResult<IOAPPDNA> UnInstallOAPP(IOAPPDNA OAPP, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return UnInstallOAPP(OAPP.OAPPId, avatarId, providerType);
        }

        public async Task<OASISResult<IOAPPDNA>> UnInstallOAPPAsync(Guid OAPPId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IEnumerable<InstalledOAPP>> intalledOAPPResult = await Data.LoadHolonsForParentAsync<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.UnInstallOAPPAsync. Reason: ";

            if (intalledOAPPResult != null && !intalledOAPPResult.IsError && intalledOAPPResult.Result != null)
            {
                InstalledOAPP installedOAPP = intalledOAPPResult.Result.FirstOrDefault(x => x.OAPPDNA.OAPPId == OAPPId);

                if (installedOAPP != null)
                {
                    OASISResult<IHolon> holonResult = await installedOAPP.DeleteAsync(false, providerType);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Message = "OAPP Uninstalled";
                        result.Result = installedOAPP.OAPPDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling DeleteAsync. Reason: {holonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed OAPP was found for the Id {OAPPId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {intalledOAPPResult.Message}");
            
            return result;
        }

        public OASISResult<IOAPPDNA> UnInstallOAPP(Guid OAPPId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPPDNA> result = new OASISResult<IOAPPDNA>();
            OASISResult<IEnumerable<InstalledOAPP>> intalledOAPPResult = Data.LoadHolonsForParent<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.UnInstallOAPP. Reason: ";

            if (intalledOAPPResult != null && !intalledOAPPResult.IsError && intalledOAPPResult.Result != null)
            {
                InstalledOAPP installedOAPP = intalledOAPPResult.Result.FirstOrDefault(x => x.OAPPDNA.OAPPId == OAPPId);

                if (installedOAPP != null)
                {
                    OASISResult<IHolon> holonResult = installedOAPP.Delete(false, providerType);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Message = "OAPP Uninstalled";
                        result.Result = installedOAPP.OAPPDNA;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling Delete. Reason: {holonResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No installed OAPP was found for the Id {OAPPId}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {intalledOAPPResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListInstalledOAPPsAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = await Data.LoadHolonsForParentAsync<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.ListInstalledOAPPsAsync. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPP>, IEnumerable<IInstalledOAPP>>(installedOAPPsResult);
                result.Result = Mapper.Convert<InstalledOAPP, IInstalledOAPP>(installedOAPPsResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        public OASISResult<IEnumerable<IInstalledOAPP>> ListInstalledOAPPs(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = Data.LoadHolonsForParent<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.ListInstalledOAPPs. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<InstalledOAPP>, IEnumerable<IInstalledOAPP>>(installedOAPPsResult);
                result.Result = Mapper.Convert<InstalledOAPP, IInstalledOAPP>(installedOAPPsResult.Result);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = await Data.LoadHolonsForParentAsync<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.IsOAPPInstalledAsync. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
                result.Result = installedOAPPsResult.Result.Any(x => x.OAPPDNA.OAPPId == OAPPId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = Data.LoadHolonsForParent<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.IsOAPPInstalled. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
                result.Result = installedOAPPsResult.Result.Any(x => x.OAPPDNA.OAPPId == OAPPId);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> IsOAPPInstalledAsync(Guid avatarId, string OAPPName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = await Data.LoadHolonsForParentAsync<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.IsOAPPInstalledAsync. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
                result.Result = installedOAPPsResult.Result.Any(x => x.OAPPDNA.OAPPName == OAPPName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsOAPPInstalled(Guid avatarId, string OAPPName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<InstalledOAPP>> installedOAPPsResult = Data.LoadHolonsForParent<InstalledOAPP>(avatarId, HolonType.InstalledOAPP, false, false, 0, true, false, 0, HolonType.All, 0, providerType);
            string errorMessage = "Error occured in OAPPManager.IsOAPPInstalled. Reason: ";

            if (installedOAPPsResult != null && !installedOAPPsResult.IsError && installedOAPPsResult.Result != null)
                result.Result = installedOAPPsResult.Result.Any(x => x.OAPPDNA.OAPPName == OAPPName);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {installedOAPPsResult.Message}");

            return result;
        }

        private IOAPPDNA ConvertOAPPToOAPPDNA(IOAPP OAPP)
        {
            return new OAPPDNA()
            {
                CelestialBodyId = OAPP.CelestialBodyId,
                CreatedByAvtarId = OAPP.CreatedByAvatarId,
                CreatedDate = OAPP.CreatedDate,
                Description = OAPP.Description,
                GenesisType = OAPP.GenesisType,
                OAPPId = OAPP.Id,
                OAPPName = OAPP.Name,
                OAPPType = OAPP.OAPPType,
                PublishedByAvatarId = OAPP.PublishedByAvatarId,
                PublishedDate = OAPP.PublishedOn,
                Version = OAPP.Version.ToString()
            };
        }

    }
}