using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4>
    {
        public OASISResult<bool> WriteDNA<T>(T STARNETDNA, string fullPathToSTARNETHolon) //where T : ISTARNETDNA
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(fullPathToSTARNETHolon))
                {
                    result.Result = true;
                    return result;
                }

                if (!Directory.Exists(fullPathToSTARNETHolon))
                    Directory.CreateDirectory(fullPathToSTARNETHolon);

                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonSerializer.Serialize(STARNETDNA));
                File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version)), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARNETHolonUIName} DNA in WriteDNA: Reason: {ex.Message}");
            }

            return result;
        }

        //public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(T STARNETDNA, string fullPathToSTARNETHolonFolder) where T : ISTARNETDNA
        public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARNETHolonFolder)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //var options = new JsonSerializerOptions
                //{
                //    WriteIndented = true,
                //    Converters = 
                //    {
                //        //new InterfaceConverter<IList<STARNETDependency>, List<STARNETDependency>>(),
                //        //new InterfaceConverter<STARNETDependency, STARNETDependency>(),
                //        //new PolymorphicConverter<STARNETDependency>(),
                //        new STARNETDependencyConvertor()
                //    }
                //};

                result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(fullPathToSTARNETDNA));

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)), options);
                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolderAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETHolonFolder)
        //public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETDNA)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPathToSTARNETDNA));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolder: Reason: {ex.Message}");
            }

            return result;
        }

        public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile)
        //public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(T STARNETDNA, string fullPathToPublishedFile) where T : ISTARNETDNA
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        //public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARNETHolonFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not the same as the version you are attempting to edit (v{storedVersion}). They must be the same if you wish to upload new files for version v{storedVersion}. Please edit the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                    return result;
                }
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS (v{storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is less than the previous version (v{storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder.");
                        return result;
                    }
                }
            }
            else if (dnaVersion != "1.0.0")
            {
                OASISErrorHandling.HandleError(ref result, $"The first version has to be 1.0.0! Please correct in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder.");
                return result;
            }

            result.Result = true;
            return result;
        }

        public virtual async Task<OASISResult<T1>> UpdateNumberOfVersionCountsAsync(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = await LoadVersionsAsync(result.Result.STARNETDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARNETHolon in installedversionsResult.Result)
                {
                    installedSTARNETHolon.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = await UpdateAsync(avatarId, installedSTARNETHolon, providerType: providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARNETHolonUIName} with Id {installedSTARNETHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public OASISResult<T1> UpdateNumberOfVersionCounts(Guid avatarId, OASISResult<T1> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> versionsResult = LoadVersions(result.Result.STARNETDNA.Id, providerType);

            if (versionsResult != null && versionsResult.Result != null && !versionsResult.IsError)
            {
                foreach (T1 holonVersion in versionsResult.Result)
                {
                    holonVersion.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T1> versionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(versionSaveResult != null && versionSaveResult.Result != null && !versionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {versionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {versionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedversionsResult = ListInstalled(avatarId, providerType);

            if (installedversionsResult != null && installedversionsResult.Result != null && !installedversionsResult.IsError)
            {
                foreach (T3 installedSTARNETHolon in installedversionsResult.Result)
                {
                    installedSTARNETHolon.STARNETDNA.NumberOfVersions = result.Result.STARNETDNA.NumberOfVersions;
                    OASISResult<T3> installedSTARSaveResult = Update(avatarId, installedSTARNETHolon, providerType: providerType);

                    if (!(installedSTARSaveResult != null && installedSTARSaveResult.Result != null && !installedSTARSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for Installed {STARNETHolonUIName} with Id {installedSTARNETHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedSTARSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the NumberOfVersions for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {versionsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T2>> UpdateDownloadCountsAsync(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARNETDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalDownloads = totalDownloads;
                downloadedSTARNETHolon.STARNETDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T2> UpdateDownloadCounts(Guid avatarId, T2 downloadedSTARNETHolon, T4 STARNETDNA, OASISResult<T2> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalDownloads = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalDownloads += holonVersion.STARNETDNA.Downloads;

                //Need to add this download (because its not saved yet).
                totalDownloads++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T1> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalDownloads = totalDownloads;
                downloadedSTARNETHolon.STARNETDNA.TotalDownloads = totalDownloads;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalDownloads = totalDownloads;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalDownloads for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total downloads for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> UpdateInstallCountsAsync(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = await LoadVersionsAsync(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARNETDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalInstalls = totalInstalls;
                installedSTARNETHolon.STARNETDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = await ListInstalledAsync(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = await ListUninstalledAsync(avatarId, providerType);

            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = await UpdateAsync(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonVersionsResult.Message}");

            return result;
        }

        public OASISResult<T3> UpdateInstallCounts(Guid avatarId, T3 installedSTARNETHolon, T4 STARNETDNA, OASISResult<T3> result, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            int totalInstalls = 0;
            OASISResult<IEnumerable<T1>> holonVersionsResult = LoadVersions(STARNETDNA.Id, providerType);

            if (holonVersionsResult != null && holonVersionsResult.Result != null && !holonVersionsResult.IsError)
            {
                //Update total installs for all versions.
                foreach (T1 holonVersion in holonVersionsResult.Result)
                    totalInstalls += holonVersion.STARNETDNA.Installs;

                //Need to add this install (because its not saved yet).
                totalInstalls++;

                foreach (T1 holonVersion in holonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T1> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }

                STARNETDNA.TotalInstalls = totalInstalls;
                installedSTARNETHolon.STARNETDNA.TotalInstalls = totalInstalls;
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all {STARNETHolonUIName} versions caused by an error in LoadSTARNETHolonVersionsAsync. Reason: {holonVersionsResult.Message}");


            OASISResult<IEnumerable<T3>> installedholonVersionsResult = ListInstalled(avatarId, providerType);

            if (installedholonVersionsResult != null && installedholonVersionsResult.Result != null && !installedholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in installedholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Installed {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {installedholonVersionsResult.Message}");

            OASISResult<IEnumerable<T3>> uninstalledholonVersionsResult = ListUninstalled(avatarId, providerType);


            if (uninstalledholonVersionsResult != null && uninstalledholonVersionsResult.Result != null && !uninstalledholonVersionsResult.IsError)
            {
                foreach (T3 holonVersion in uninstalledholonVersionsResult.Result)
                {
                    holonVersion.STARNETDNA.TotalInstalls = totalInstalls;
                    OASISResult<T3> holonVersionSaveResult = Update(avatarId, holonVersion, providerType: providerType);

                    if (!(holonVersionSaveResult != null && holonVersionSaveResult.Result != null && !holonVersionSaveResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the TotalInstalls for Uninstalled {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {holonVersionSaveResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the total installs for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {uninstalledholonVersionsResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<(T1 parent, string installPath)> dependencyResult = AddDependency(parent, installedDependency, dependencyType, errorMessage, installDependency, dependencyInstallMode);

                if (dependencyResult != null && dependencyResult.Result.parent != null && !dependencyResult.IsError)
                {
                    result = await UpdateAsync(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && installDependency)
                    {
                        DirectoryHelper.CopyFilesRecursively(installedDependency.InstalledPath, dependencyResult.Result.installPath);
                        
                        // Generate proxy class if this is a Library dependency added to an OAPP
                        if (dependencyType == DependencyType.Library && parent.HolonType == HolonType.OAPP)
                        {
                            await GenerateLibraryProxyForOAPPAsync(parent, installedDependency, dependencyResult.Result.installPath);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with AddDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<(T1 parent, string installPath)> dependencyResult = AddDependency(parent, installedDependency, dependencyType, errorMessage, installDependency, dependencyInstallMode);

                if (dependencyResult != null && dependencyResult.Result.parent != null && !dependencyResult.IsError)
                {
                    result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && installDependency)
                    {
                        DirectoryHelper.CopyFilesRecursively(installedDependency.InstalledPath, dependencyResult.Result.installPath);
                        
                        // Generate proxy class if this is a Library dependency added to an OAPP
                        if (dependencyType == DependencyType.Library && parent.HolonType == HolonType.OAPP)
                        {
                            var proxyTask = GenerateLibraryProxyForOAPPAsync(parent, installedDependency, dependencyResult.Result.installPath);
                            proxyTask.Wait(); // Wait for proxy generation
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with AddDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddDependency(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await AddDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return AddDependency(avatarId, parentResult.Result, installedDependency, dependencyType, installDependency, dependencyInstallMode, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await AddDependencyAsync<T>(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = AddDependency(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> AddDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion}

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await AddDependencyAsync(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> AddDependency<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, bool installDependency = true, DependencyInstallMode dependencyInstallMode = DependencyInstallMode.Nested, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.AddDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = AddDependency(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, installDependency, dependencyInstallMode, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }


        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<STARNETDependency> dependencyResult = RemoveDependency(parent, installedDependency, dependencyType, errorMessage);

                if (dependencyResult != null && dependencyResult.Result != null && !dependencyResult.IsError)
                {
                    result = await UpdateAsync(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (Directory.Exists(dependencyResult.Result.InstalledTo))
                            Directory.Delete(dependencyResult.Result.InstalledTo, true);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with RemoveDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, T1 parent, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<STARNETDependency> dependencyResult = RemoveDependency(parent, installedDependency, dependencyType, errorMessage);

                if (dependencyResult != null && dependencyResult.Result != null && !dependencyResult.IsError)
                {
                    result = Update(avatarId, parent, result, errorMessage, true, string.Concat(Enum.GetName(typeof(HolonType), parent.HolonType), "DNAJSON"), providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (Directory.Exists(dependencyResult.Result.InstalledTo))
                            Directory.Delete(dependencyResult.Result.InstalledTo, true);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured adding the dependency with RemoveDependency. Reason: {dependencyResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await RemoveDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, string parentVersion, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "Version", parentVersion }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return RemoveDependency(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            try
            {
                OASISResult<T1> parentResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return await RemoveDependencyAsync(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaDataAsync. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, T installedDependency, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            try
            {
                OASISResult<T1> parentResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
                {
                    { STARNETHolonIdName, parentId.ToString() },
                    { "VersionSequence", parentVersionSequence.ToString() }

                }, MetaKeyValuePairMatchMode.All, STARNETHolonType, providerType: providerType);

                if (parentResult != null && parentResult.Result != null && !parentResult.IsError)
                    return RemoveDependency(avatarId, parentResult.Result, installedDependency, dependencyType, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the parent with OAPPManagerBase.LoadHolonByMetaData. Reason: {parentResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await RemoveDependencyAsync<T>(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, int parentVersionSequence, Guid dependencyId, int dependencyVersionSequence, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "VersionSequence", dependencyVersionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = RemoveDependency(avatarId, parentId, parentVersionSequence, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T1>> RemoveDependencyAsync<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependencyAsync. Reason:";

            OASISResult<T> installedDependencyResult = await Data.LoadHolonByMetaDataAsync<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion}

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = await RemoveDependencyAsync(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaDataAsync. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public OASISResult<T1> RemoveDependency<T>(Guid avatarId, Guid parentId, string parentVersion, Guid dependencyId, string dependencyVersion, HolonType dependencyHolonType, DependencyType dependencyType, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon, new()
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in OAPPManagerBase.RemoveDependency. Reason:";

            OASISResult<T> installedDependencyResult = Data.LoadHolonByMetaData<T>(new Dictionary<string, string>()
            {
                { "STARNETHolonId", dependencyId.ToString() },
                { "Version", dependencyVersion }

            }, MetaKeyValuePairMatchMode.All, dependencyHolonType, providerType: providerType);

            if (installedDependencyResult != null && installedDependencyResult.Result != null && !installedDependencyResult.IsError)
                result = RemoveDependency(avatarId, parentId, parentVersion, installedDependencyResult.Result, dependencyType, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the Installed Dependency with Data.LoadHolonByMetaData. Reason: {installedDependencyResult.Message}");

            return result;
        }

        public async Task<OASISResult<T>> InstallDependencyAsync<T>(Guid avatarId, STARNETDependency dependency, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon
        {
            OASISResult<T> result = new OASISResult<T>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(defaultDownloadPath) || string.IsNullOrEmpty(STARDNA.STARNETBasePath))
                downloadPath = defaultDownloadPath;
            else
                downloadPath = Path.Combine(STARDNA.STARNETBasePath, defaultDownloadPath);

            if (Path.IsPathRooted(defaultInstallPath) || string.IsNullOrEmpty(STARDNA.STARNETBasePath))
                installPath = defaultInstallPath;
            else
                installPath = Path.Combine(STARDNA.STARNETBasePath, defaultInstallPath);

            switch (dependency.Type)
            {
                case DependencyType.Runtime:
                    {
                        RuntimeManager runtimeManager = new RuntimeManager(avatarId, STARDNA, OASISDNA);
                        runtimeManager.OnDownloadStatusChanged += RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged += RuntimeManager_OnInstallStatusChanged;
                        OASISResult<InstalledRuntime> installResult = await runtimeManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        runtimeManager.OnDownloadStatusChanged -= RuntimeManager_OnDownloadStatusChanged;
                        runtimeManager.OnInstallStatusChanged -= RuntimeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        runtimeManager = null;
                    }
                    break;

                case DependencyType.Library:
                    {
                        LibraryManager libManager = new LibraryManager(avatarId, STARDNA, OASISDNA);
                        libManager.OnDownloadStatusChanged += LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged += LibManager_OnInstallStatusChanged;
                        OASISResult<InstalledLibrary> installResult = await libManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        libManager.OnDownloadStatusChanged -= LibManager_OnDownloadStatusChanged;
                        libManager.OnInstallStatusChanged -= LibManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        libManager = null;
                    }
                    break;

                case DependencyType.Template:
                    {
                        OAPPTemplateManager templateManager = new OAPPTemplateManager(avatarId, STARDNA, OASISDNA);
                        templateManager.OnDownloadStatusChanged += TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged += TemplateManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPPTemplate> installResult = await templateManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        templateManager.OnDownloadStatusChanged -= TemplateManager_OnDownloadStatusChanged;
                        templateManager.OnInstallStatusChanged -= TemplateManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        templateManager = null;
                    }
                    break;

                case DependencyType.OAPP:
                    {
                        OAPPManager OAPPManager = new OAPPManager(avatarId, STARDNA, OASISDNA);
                        OAPPManager.OnDownloadStatusChanged += OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged += OAPPManager_OnInstallStatusChanged;
                        OASISResult<InstalledOAPP> installResult = await OAPPManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        OAPPManager.OnDownloadStatusChanged -= OAPPManager_OnDownloadStatusChanged;
                        OAPPManager.OnInstallStatusChanged -= OAPPManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        OAPPManager = null;
                    }
                    break;

                case DependencyType.Quest:
                    {
                        QuestManager QuestManager = new QuestManager(avatarId, STARDNA, OASISDNA);
                        QuestManager.OnDownloadStatusChanged += QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged += QuestManager_OnInstallStatusChanged;
                        OASISResult<InstalledQuest> installResult = await QuestManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        QuestManager.OnDownloadStatusChanged -= QuestManager_OnDownloadStatusChanged;
                        QuestManager.OnInstallStatusChanged -= QuestManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        QuestManager = null;
                    }
                    break;

                case DependencyType.Mission:
                    {
                        MissionManager MissionManager = new MissionManager(avatarId, STARDNA, OASISDNA);
                        MissionManager.OnDownloadStatusChanged += MissionManager_OnDownloadStatusChanged;
                        MissionManager.OnInstallStatusChanged += MissionManager_OnInstallStatusChanged;
                        OASISResult<InstalledMission> installResult = await MissionManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        MissionManager.OnDownloadStatusChanged -= MissionManager_OnDownloadStatusChanged;
                        MissionManager.OnInstallStatusChanged -= MissionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        MissionManager = null;
                    }
                    break;

                case DependencyType.Chapter:
                    {
                        ChapterManager ChapterManager = new ChapterManager(avatarId, STARDNA, OASISDNA);
                        ChapterManager.OnDownloadStatusChanged += ChapterManager_OnDownloadStatusChanged;
                        ChapterManager.OnInstallStatusChanged += ChapterManager_OnInstallStatusChanged;
                        OASISResult<InstalledChapter> installResult = await ChapterManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ChapterManager.OnDownloadStatusChanged -= ChapterManager_OnDownloadStatusChanged;
                        ChapterManager.OnInstallStatusChanged -= ChapterManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ChapterManager = null;
                    }
                    break;

                case DependencyType.NFT:
                    {
                        STARNFTManager NFTManager = new STARNFTManager(avatarId, STARDNA, OASISDNA);
                        NFTManager.OnDownloadStatusChanged += NFTManager_OnDownloadStatusChanged;
                        NFTManager.OnInstallStatusChanged += NFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFT> installResult = await NFTManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        NFTManager.OnDownloadStatusChanged -= NFTManager_OnDownloadStatusChanged;
                        NFTManager.OnInstallStatusChanged -= NFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        NFTManager = null;
                    }
                    break;

                case DependencyType.GeoNFT:
                    {
                        STARGeoNFTManager GeoNFTManager = new STARGeoNFTManager(avatarId, STARDNA, OASISDNA);
                        GeoNFTManager.OnDownloadStatusChanged += GeoNFTManager_OnDownloadStatusChanged;
                        GeoNFTManager.OnInstallStatusChanged += GeoNFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoNFT> installResult = await GeoNFTManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        GeoNFTManager.OnDownloadStatusChanged -= GeoNFTManager_OnDownloadStatusChanged;
                        GeoNFTManager.OnInstallStatusChanged -= GeoNFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        GeoNFTManager = null;
                    }
                    break;

                case DependencyType.NFTCollection:
                    {
                        STARNFTCollectionManager STARNFTCollectionManager = new STARNFTCollectionManager(avatarId, STARDNA, OASISDNA);
                        STARNFTCollectionManager.OnDownloadStatusChanged += NFTCollectionManager_OnDownloadStatusChanged;
                        STARNFTCollectionManager.OnInstallStatusChanged += NFTCollectionManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFTCollection> installResult = await STARNFTCollectionManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARNFTCollectionManager.OnDownloadStatusChanged -= NFTCollectionManager_OnDownloadStatusChanged;
                        STARNFTCollectionManager.OnInstallStatusChanged -= NFTCollectionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARNFTCollectionManager = null;
                    }
                    break;

                case DependencyType.GeoNFTCollection:
                    {
                        STARGeoNFTCollectionManager STARGeoNFTCollectionManager = new STARGeoNFTCollectionManager(avatarId, STARDNA, OASISDNA);
                        STARGeoNFTCollectionManager.OnDownloadStatusChanged += GeoNFTCollectionManager_OnDownloadStatusChanged;
                        STARGeoNFTCollectionManager.OnInstallStatusChanged += GeoNFTCollectionManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoNFTCollection> installResult = await STARGeoNFTCollectionManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARGeoNFTCollectionManager.OnDownloadStatusChanged -= GeoNFTCollectionManager_OnDownloadStatusChanged;
                        STARGeoNFTCollectionManager.OnInstallStatusChanged -= GeoNFTCollectionManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARGeoNFTCollectionManager = null;
                    }
                    break;

                case DependencyType.GeoHotSpot:
                    {
                        GeoHotSpotManager GeoHotSpotManager = new GeoHotSpotManager(avatarId, STARDNA, OASISDNA);
                        GeoHotSpotManager.OnDownloadStatusChanged += GeoHotSpotManager_OnDownloadStatusChanged;
                        GeoHotSpotManager.OnInstallStatusChanged += GeoHotSpotManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoHotSpot> installResult = await GeoHotSpotManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        GeoHotSpotManager.OnDownloadStatusChanged -= GeoHotSpotManager_OnDownloadStatusChanged;
                        GeoHotSpotManager.OnInstallStatusChanged -= GeoHotSpotManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        GeoHotSpotManager = null;
                    }
                    break;

                case DependencyType.CelestialSpace:
                    {
                        CelestialSpaceManager CelestialSpaceManager = new CelestialSpaceManager(avatarId, STARDNA, OASISDNA);
                        CelestialSpaceManager.OnDownloadStatusChanged += CelestialSpaceManager_OnDownloadStatusChanged;
                        CelestialSpaceManager.OnInstallStatusChanged += CelestialSpaceManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialSpace> installResult = await CelestialSpaceManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialSpaceManager.OnDownloadStatusChanged -= CelestialSpaceManager_OnDownloadStatusChanged;
                        CelestialSpaceManager.OnInstallStatusChanged -= CelestialSpaceManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialSpaceManager = null;
                    }
                    break;

                case DependencyType.CelestialBody:
                    {
                        CelestialBodyManager CelestialBodyManager = new CelestialBodyManager(avatarId, STARDNA, OASISDNA);
                        CelestialBodyManager.OnDownloadStatusChanged += CelestialBodyManager_OnDownloadStatusChanged;
                        CelestialBodyManager.OnInstallStatusChanged += CelestialBodyManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialBody> installResult = await CelestialBodyManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialBodyManager.OnDownloadStatusChanged -= CelestialBodyManager_OnDownloadStatusChanged;
                        CelestialBodyManager.OnInstallStatusChanged -= CelestialBodyManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialBodyManager = null;
                    }
                    break;

                case DependencyType.Zome:
                    {
                        STARZomeManager ZomeManager = new STARZomeManager(avatarId, STARDNA, OASISDNA);
                        ZomeManager.OnDownloadStatusChanged += ZomeManager_OnDownloadStatusChanged;
                        ZomeManager.OnInstallStatusChanged += ZomeManager_OnInstallStatusChanged;
                        OASISResult<InstalledZome> installResult = await ZomeManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ZomeManager.OnDownloadStatusChanged -= ZomeManager_OnDownloadStatusChanged;
                        ZomeManager.OnInstallStatusChanged -= ZomeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ZomeManager = null;
                    }
                    break;

                case DependencyType.Holon:
                    {
                        STARHolonManager HolonManager = new STARHolonManager(avatarId, STARDNA, OASISDNA);
                        HolonManager.OnDownloadStatusChanged += HolonManager_OnDownloadStatusChanged;
                        HolonManager.OnInstallStatusChanged += HolonManager_OnInstallStatusChanged;
                        OASISResult<InstalledHolon> installResult = await HolonManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        HolonManager.OnDownloadStatusChanged -= HolonManager_OnDownloadStatusChanged;
                        HolonManager.OnInstallStatusChanged -= HolonManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        HolonManager = null;
                    }
                    break;

                case DependencyType.InventoryItem:
                    {
                        InventoryItemManager InventoryItemManager = new InventoryItemManager(avatarId, STARDNA, OASISDNA);
                        InventoryItemManager.OnDownloadStatusChanged += InventoryItemManager_OnDownloadStatusChanged;
                        InventoryItemManager.OnInstallStatusChanged += InventoryItemManager_OnInstallStatusChanged;
                        OASISResult<InstalledInventoryItem> installResult = await InventoryItemManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        InventoryItemManager.OnDownloadStatusChanged -= InventoryItemManager_OnDownloadStatusChanged;
                        InventoryItemManager.OnInstallStatusChanged -= InventoryItemManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        InventoryItemManager = null;
                    }
                    break;

                case DependencyType.CelestialBodyMetaDataDNA:
                    {
                        CelestialBodyMetaDataDNAManager CelestialBodyMetaDataDNAManager = new CelestialBodyMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        CelestialBodyMetaDataDNAManager.OnDownloadStatusChanged += CelestialBodyMetaDataDNAManager_OnDownloadStatusChanged;
                        CelestialBodyMetaDataDNAManager.OnInstallStatusChanged += CelestialBodyMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledCelestialBodyMetaDataDNA> installResult = await CelestialBodyMetaDataDNAManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialBodyMetaDataDNAManager.OnDownloadStatusChanged -= CelestialBodyMetaDataDNAManager_OnDownloadStatusChanged;
                        CelestialBodyMetaDataDNAManager.OnInstallStatusChanged -= CelestialBodyMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialBodyMetaDataDNAManager = null;
                    }
                    break;

                case DependencyType.ZomeMetaDataDNA:
                    {
                        ZomeMetaDataDNAManager ZomeMetaDataDNAManager = new ZomeMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        ZomeMetaDataDNAManager.OnDownloadStatusChanged += ZomeMetaDataDNAManager_OnDownloadStatusChanged;
                        ZomeMetaDataDNAManager.OnInstallStatusChanged += ZomeMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledZomeMetaDataDNA> installResult = await ZomeMetaDataDNAManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ZomeMetaDataDNAManager.OnDownloadStatusChanged -= ZomeMetaDataDNAManager_OnDownloadStatusChanged;
                        ZomeMetaDataDNAManager.OnInstallStatusChanged -= ZomeMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ZomeMetaDataDNAManager = null;
                    }
                    break;

                case DependencyType.HolonMetaDataDNA:
                    {
                        HolonMetaDataDNAManager HolonMetaDataDNAManager = new HolonMetaDataDNAManager(avatarId, STARDNA, OASISDNA);
                        HolonMetaDataDNAManager.OnDownloadStatusChanged += HolonMetaDataDNAManager_OnDownloadStatusChanged;
                        HolonMetaDataDNAManager.OnInstallStatusChanged += HolonMetaDataDNAManager_OnInstallStatusChanged;
                        OASISResult<InstalledHolonMetaDataDNA> installResult = await HolonMetaDataDNAManager.DownloadAndInstallAsync(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        HolonMetaDataDNAManager.OnDownloadStatusChanged -= HolonMetaDataDNAManager_OnDownloadStatusChanged;
                        HolonMetaDataDNAManager.OnInstallStatusChanged -= HolonMetaDataDNAManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        HolonMetaDataDNAManager = null;
                    }
                    break;

                default:
                    {
                        OASISErrorHandling.HandleError(ref result, $"Unsupported dependency type: {dependency.Type} for dependency {dependency.Name}.");
                    }
                    break;
            }

            return result;
        }

    }
}
