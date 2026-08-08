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
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4> where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
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

    }
}