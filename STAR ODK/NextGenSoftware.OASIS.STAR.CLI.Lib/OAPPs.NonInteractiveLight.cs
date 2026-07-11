using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.Utilities.ExtentionMethods;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class OAPPs
    {
        /// <summary>Loads <see cref="StarCliLightRequest"/> from JSON and runs the same pipeline as the interactive Light wizard: <see cref="Star.LightAsync"/> then STARNET OAPP registration, dependency copy, runtime install, DNA refresh.</summary>
        public async Task<OASISResult<CoronalEjection>> LightFromJsonFileAsync(string jsonPath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<CoronalEjection> result = new OASISResult<CoronalEjection>();

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                OASISErrorHandling.HandleError(ref result, "Light JSON path is empty.");
                return result;
            }

            if (!File.Exists(jsonPath))
            {
                OASISErrorHandling.HandleError(ref result, $"Light JSON file not found: {jsonPath}");
                return result;
            }

            StarCliLightRequest req;
            try
            {
                req = JsonConvert.DeserializeObject<StarCliLightRequest>(File.ReadAllText(jsonPath));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to parse Light JSON: {ex.Message}");
                return result;
            }

            if (req == null)
            {
                OASISErrorHandling.HandleError(ref result, "Light JSON deserialized to null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(req.OAPPName) || string.IsNullOrWhiteSpace(req.OAPPDescription))
            {
                OASISErrorHandling.HandleError(ref result, "Light JSON requires oappName and oappDescription.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(req.GenesisType) || !Enum.TryParse(req.GenesisType, true, out GenesisType genesisType))
            {
                OASISErrorHandling.HandleError(ref result, $"Light JSON requires a valid genesisType (e.g. Moon, Planet, Star). Got: {req.GenesisType}");
                return result;
            }

            ProviderType pt = providerType;
            if (!string.IsNullOrWhiteSpace(req.ProviderType) && Enum.TryParse(req.ProviderType, true, out ProviderType parsedPt))
                pt = parsedPt;

            OAPPType oappType = OAPPType.OAPPTemplate;
            if (!string.IsNullOrWhiteSpace(req.OAPPType) && Enum.TryParse(req.OAPPType, true, out OAPPType parsedOapp))
                oappType = parsedOapp;

            if (!req.UseOappTemplate)
                oappType = OAPPType.GeneratedCodeOnly;

            OAPPTemplateType templateTypeEnum = OAPPTemplateType.Console;
            if (!string.IsNullOrWhiteSpace(req.OAPPTemplateType))
                Enum.TryParse(req.OAPPTemplateType, true, out templateTypeEnum);

            Guid templateGuid = Guid.Empty;
            int versionSeq = 0;
            IInstalledOAPPTemplate installedTemplate = null;

            if (oappType != OAPPType.GeneratedCodeOnly)
            {
                if (!req.OAPPTemplateId.HasValue || req.OAPPTemplateId.Value == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Light JSON requires oappTemplateId when useOappTemplate is true and oappType is not GeneratedCodeOnly.");
                    return result;
                }

                templateGuid = req.OAPPTemplateId.Value;
                int templateVersion = req.OAPPTemplateVersion ?? 1;
                versionSeq = req.OAPPTemplateVersionSequence ?? req.OAPPTemplateVersion ?? 1;

                OASISResult<InstalledOAPPTemplate> loadTpl = await STAR.STARAPI.OAPPTemplates.LoadInstalledAsync(STAR.BeamedInAvatar.Id, templateGuid, true, templateVersion, pt);
                if (loadTpl == null || loadTpl.IsError || loadTpl.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadTpl?.Message ?? "Could not load installed OAPP template for Light.");
                    return result;
                }

                installedTemplate = loadTpl.Result;
            }

            string dnaFolder = req.CelestialBodyDnaFolder?.Trim() ?? "";
            string genesisFolderRoot;
            if (!string.IsNullOrWhiteSpace(req.GenesisFolder))
                genesisFolderRoot = req.GenesisFolder.Trim();
            else if (!string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                genesisFolderRoot = Path.Combine(STAR.STARDNA.STARNETBasePath, STAR.STARDNA.DefaultOAPPsSourcePath);
            else
                genesisFolderRoot = STAR.STARDNA.DefaultOAPPsSourcePath;

            string genesisNamespace = string.IsNullOrWhiteSpace(req.GenesisNamespace)
                ? req.OAPPName.ToPascalCase()
                : req.GenesisNamespace.Trim();

            List<MetaHolonTag> metaHolon = req.MetaHolonTags ?? new List<MetaHolonTag>();
            Dictionary<string, string> metaTag = req.MetaTags ?? new Dictionary<string, string>();

            byte[] ourWorld3d = TryReadOptionalFile(req.OurWorld3dObjectPath);
            byte[] ourWorld2d = TryReadOptionalFile(req.OurWorld2dSpritePath);
            byte[] oneWorld3d = TryReadOptionalFile(req.OneWorld3dObjectPath);
            byte[] oneWorld2d = TryReadOptionalFile(req.OneWorld2dSpritePath);
            Uri ourWorld3dUri = TryParseOptionalUri(req.OurWorld3dObjectUri);
            Uri ourWorld2dUri = TryParseOptionalUri(req.OurWorld2dSpriteUri);
            Uri oneWorld3dUri = TryParseOptionalUri(req.OneWorld3dObjectUri);
            Uri oneWorld2dUri = TryParseOptionalUri(req.OneWorld2dSpriteUri);

            if (!CLIEngine.JsonOutput)
                CLIEngine.ShowWorkingMessage("Generating OAPP (non-interactive Light)...");

            OASISResult<CoronalEjection> lightResult;
            Guid parentId = req.ParentCelestialBodyId ?? Guid.Empty;

            if (parentId != Guid.Empty)
            {
                lightResult = await STAR.LightAsync(req.OAPPName, req.OAPPDescription, oappType, templateGuid, versionSeq, genesisType,
                    dnaFolder, genesisFolderRoot, genesisNamespace, metaHolon, metaTag, parentId, pt);
            }
            else
            {
                lightResult = await STAR.LightAsync(req.OAPPName, req.OAPPDescription, oappType, templateGuid, versionSeq, genesisType,
                    dnaFolder, genesisFolderRoot, genesisNamespace, metaHolon, metaTag, (ICelestialBody)null, pt);
            }

            if (lightResult == null || lightResult.IsError || lightResult.Result == null)
            {
                if (lightResult != null)
                    return lightResult;
                OASISErrorHandling.HandleError(ref result, "STAR.LightAsync returned null.");
                return result;
            }

            result = lightResult;

            if (req.SkipStarnetOappCreate)
                return result;

            if (lightResult.Result.CelestialBody == null)
            {
                OASISErrorHandling.HandleError(ref result, "Light completed without a CelestialBody (e.g. genesisType ZomesAndHolonsOnly). Set skipStarnetOappCreate to true or use a Moon/Planet/Star-style genesis. STARNET OAPP registration was skipped.");
                return result;
            }

            string oappPath = Path.Combine(genesisFolderRoot, req.OAPPName);
            string cbMetaPath = string.IsNullOrWhiteSpace(req.CelestialBodyMetaDataGeneratedPath)
                ? dnaFolder
                : req.CelestialBodyMetaDataGeneratedPath.Trim();

            OASISResult<OAPP> createOAPPResult = await CreateOappStarnetRecordAfterLightAsync(
                lightResult,
                req.OAPPName,
                req.OAPPDescription,
                oappType,
                genesisType,
                templateTypeEnum,
                installedTemplate,
                oappPath,
                genesisNamespace,
                cbMetaPath,
                metaHolon,
                metaTag,
                req.OurWorldLat,
                req.OurWorldLong,
                ourWorld3d,
                ourWorld3dUri,
                ourWorld2d,
                ourWorld2dUri,
                req.OneWorldLat,
                req.OneWorldLong,
                oneWorld3d,
                oneWorld3dUri,
                oneWorld2d,
                oneWorld2dUri,
                pt);

            if (createOAPPResult == null || createOAPPResult.IsError || createOAPPResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, createOAPPResult?.Message ?? "STARNET OAPP.CreateAsync failed after Light.");
                return result;
            }

            result.Result.OAPP = createOAPPResult.Result;

            if (installedTemplate != null && createOAPPResult.Result.STARNETDNA != null && installedTemplate.STARNETDNA != null)
                createOAPPResult.Result.STARNETDNA.Dependencies = installedTemplate.STARNETDNA.Dependencies;

            OASISResult<OAPP> saveResult = await STARNETManager.UpdateAsync(STAR.BeamedInAvatar.Id, createOAPPResult.Result, true, providerType: pt);
            if (saveResult == null || saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, saveResult?.Message ?? "Failed to save OAPP after Light.");
                return result;
            }

            OASISResult<bool> installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(
                result.Result.OAPP.STARNETDNA, oappPath, InstallRuntimesFor.OAPP, pt);

            if (installRuntimesResult == null || !installRuntimesResult.Result || installRuntimesResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, installRuntimesResult?.Message ?? "Installing dependent runtimes failed after Light.");
                result.IsError = true;
                return result;
            }

            if (!CLIEngine.JsonOutput)
                CLIEngine.ShowSuccessMessage(string.IsNullOrEmpty(result.Message) ? "OAPP Successfully Generated." : $"OAPP Successfully Generated. ({result.Message})");

            OASISResult<STARNETDNA> dnaResult = await STARNETManager.ReadDNAFromSourceOrInstallFolderAsync<STARNETDNA>(result.Result.OAPP.STARNETDNA.SourcePath);
            if (dnaResult != null && dnaResult.Result != null && !dnaResult.IsError)
                result.Result.OAPP.STARNETDNA = dnaResult.Result;
            else if (!CLIEngine.JsonOutput)
                CLIEngine.ShowErrorMessage($"Warning: could not refresh STARNETDNA: {dnaResult?.Message}");

            await AddDependenciesAsync(result.Result.OAPP.STARNETDNA, pt);

            if (!CLIEngine.NonInteractive && !CLIEngine.JsonOutput)
            {
                Console.WriteLine("");
                await ShowAsync(result.Result.OAPP, customData: result.Result.CelestialBody.CelestialBodyCore.Zomes);
                Console.WriteLine("");
            }

            return result;
        }

        private static byte[] TryReadOptionalFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            try
            {
                path = path.Trim();
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            catch
            {
                /* optional asset */
            }
            return null;
        }

        private static Uri TryParseOptionalUri(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;
            return Uri.TryCreate(s.Trim(), UriKind.Absolute, out Uri u) ? u : null;
        }

        private static async Task<OASISResult<OAPP>> CreateOappStarnetRecordAfterLightAsync(
            OASISResult<CoronalEjection> lightResult,
            string oappName,
            string oappDesc,
            OAPPType oappType,
            GenesisType genesisType,
            OAPPTemplateType oapPTemplateType,
            IInstalledOAPPTemplate installedOAPPTemplate,
            string oappPath,
            string genesisNamespace,
            string cbMetaDataGeneratedPath,
            List<MetaHolonTag> metaHolonTagMappings,
            Dictionary<string, string> metaTagMappings,
            long ourWorldLat,
            long ourWorldLong,
            byte[] ourWorld3dObject,
            Uri ourWorld3dObjectURI,
            byte[] ourWorld2dSprite,
            Uri ourWorld2dSpriteURI,
            long oneWorldLat,
            long oneWorldLong,
            byte[] oneWorld3dObject,
            Uri oneWorld3dObjectURI,
            byte[] oneWorld2dSprite,
            Uri oneWorld2dSpriteURI,
            ProviderType providerType)
        {
            Guid tplId = installedOAPPTemplate?.STARNETDNA?.Id ?? Guid.Empty;
            string tplName = installedOAPPTemplate?.STARNETDNA?.Name;
            string tplDesc = installedOAPPTemplate?.STARNETDNA?.Description;
            int tplVerSeq = installedOAPPTemplate?.STARNETDNA?.VersionSequence ?? 0;
            string tplVer = installedOAPPTemplate?.STARNETDNA?.Version;
            string tplPath = installedOAPPTemplate?.InstalledPath;

            var metaBlock = new Dictionary<string, object>
            {
                { "CelestialBodyId", lightResult.Result.CelestialBody.Id },
                { "CelestialBodyName", lightResult.Result.CelestialBody.Name },
                { "GenesisType", genesisType },
                { "OAPPTemplateId", tplId == Guid.Empty ? null : (object)tplId },
                { "OAPPTemplateName", tplName },
                { "OAPPTemplateDescription", tplDesc },
                { "OAPPTemplateType", oapPTemplateType },
                { "OAPPTemplateVersion", tplVer },
                { "OAPPTemplateVersionSequence", tplVerSeq },
                { "OAPPTemplateInstalledPath", tplPath },
                { "CelestialBodyMetaDataId", null },
                { "CelestialBodyMetaDataName", null },
                { "CelestialBodyMetaDataDescription", null },
                { "CelestialBodyMetaDataType", null },
                { "CelestialBodyMetaDataVersionSequence", null },
                { "CelestialBodyMetaDataVersion", null },
                { "CelestialBodyMetaDataInstalledPath", null },
                { "CelestialBodyMetaDataGeneratedPath", cbMetaDataGeneratedPath },
                { "STARNETHolonType", oappType },
                { "OurWorldLat", ourWorldLat },
                { "OurWorldLong", ourWorldLong },
                { "OurWorld3dObject", ourWorld3dObject },
                { "OurWorld3dObjectURI", ourWorld3dObjectURI },
                { "OurWorld2dSprite", ourWorld2dSprite },
                { "OurWorld2dSpriteURI", ourWorld2dSpriteURI },
                { "OneWorldLat", oneWorldLat },
                { "OneWorldLong", oneWorldLong },
                { "OneWorld3dObject", oneWorld3dObject },
                { "OneWorld3dObjectURI", oneWorld3dObjectURI },
                { "OneWorld2dSprite", oneWorld2dSprite },
                { "OneWorld2dSpriteURI", oneWorld2dSpriteURI }
            };

            var oappHolon = new OAPP
            {
                Name = oappName,
                Description = oappDesc,
                GenesisType = genesisType,
                CelestialBodyId = lightResult.Result.CelestialBody.Id,
                CelestialBodyName = lightResult.Result.CelestialBody.Name,
                OAPPTemplateId = tplId,
                OAPPTemplateName = tplName,
                OAPPTemplateDescription = tplDesc,
                OAPPTemplateType = oapPTemplateType,
                OAPPTemplateVersion = tplVer,
                OAPPTemplateVersionSequence = tplVerSeq,
                CelestialBodyMetaDataId = Guid.Empty,
                CelestialBodyMetaDataGeneratedPath = cbMetaDataGeneratedPath,
                OurWorldLat = ourWorldLat,
                OurWorldLong = ourWorldLong,
                OurWorld3dObject = ourWorld3dObject,
                OurWorld3dObjectURI = ourWorld3dObjectURI,
                OurWorld2dSprite = ourWorld2dSprite,
                OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                OneWorldLat = oneWorldLat,
                OneWorldLong = oneWorldLong,
                OneWorld3dObject = oneWorld3dObject,
                OneWorld3dObjectURI = oneWorld3dObjectURI,
                OneWorld2dSprite = oneWorld2dSprite,
                OneWorld2dSpriteURI = oneWorld2dSpriteURI
            };

            return await STAR.STARAPI.OAPPs.CreateAsync(STAR.BeamedInAvatar.Id, oappName, oappDesc, oappType, oappPath,
                new STARNETCreateOptions<OAPP, STARNETDNA>
                {
                    MetaTagMappings = new MetaTagMappings
                    {
                        MetaHolonTags = metaHolonTagMappings,
                        MetaTags = metaTagMappings
                    },
                    STARNETDNA = new STARNETDNA { MetaData = new Dictionary<string, object>(metaBlock) },
                    STARNETHolon = oappHolon
                }, providerType);
        }

        private static OASISResult<OAPP> MapCoronalToOapp(OASISResult<CoronalEjection> cor)
        {
            return new OASISResult<OAPP>
            {
                IsError = cor.IsError,
                Message = cor.Message,
                Result = cor.Result?.OAPP != null ? (OAPP)cor.Result.OAPP : null
            };
        }
    }
}
