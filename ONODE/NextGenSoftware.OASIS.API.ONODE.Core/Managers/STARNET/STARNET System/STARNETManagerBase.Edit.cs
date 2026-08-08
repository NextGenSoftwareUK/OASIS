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
        public virtual async Task<OASISResult<T1>> EditAsync(Guid id, T4 newSTARNETDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(id, avatarId, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                await EditAsync(avatarId, loadResult.Result, newSTARNETDNA, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.EditAsync. Reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> EditAsync(Guid avatarId, T1 holon, T4 newSTARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in STARNETManagerBase.EditAsync. Reason: ";
            string oldPath = "";
            string newPath = "";
            string oldPublishedPath = "";
            string oldDownloadedPath = "";
            string oldInstalledPath = "";
            string oldName = "";
            string launchTarget = "";

            if (holon.Name != newSTARNETDNA.Name)
            {
                oldName = holon.Name;
                oldPath = holon.STARNETDNA.SourcePath;
                newPath = Path.Combine(new DirectoryInfo(holon.STARNETDNA.SourcePath).Parent.FullName, newSTARNETDNA.Name);
                newSTARNETDNA.SourcePath = newPath;

                if (newSTARNETDNA.LaunchTarget != null)
                    newSTARNETDNA.LaunchTarget = newSTARNETDNA.LaunchTarget.Replace(holon.Name, newSTARNETDNA.Name);
                
                launchTarget = newSTARNETDNA.LaunchTarget;

                holon.MetaData[STARNETHolonNameName] = newSTARNETDNA.Name;

                if (!string.IsNullOrEmpty(holon.STARNETDNA.PublishedPath))
                {
                    oldPublishedPath = holon.STARNETDNA.PublishedPath;
                    newSTARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
                }
            }

            holon.STARNETDNA = newSTARNETDNA;
            holon.Name = newSTARNETDNA.Name;
            holon.Description = newSTARNETDNA.Description;

            if (!string.IsNullOrEmpty(newPath) && !string.IsNullOrEmpty(oldPath))
            {
                try
                {
                    if (Directory.Exists(oldPath))
                        Directory.Move(oldPath, newPath);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                }

                if (!string.IsNullOrEmpty(newSTARNETDNA.PublishedPath))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPublishedPath) && File.Exists(oldPublishedPath))
                            File.Move(oldPublishedPath, newSTARNETDNA.PublishedPath);
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                        CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                    }
                }
            }

            OASISResult<T1> saveResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (saveResult != null && !saveResult.IsError && saveResult.Result != null)
            {
                OASISResult<IEnumerable<T1>> holonsResult = await LoadVersionsAsync(newSTARNETDNA.Id, providerType);

                if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                {
                    foreach (T1 holonVersion in holonsResult.Result)
                    {
                        //No need to update the version we already updated above.
                        if (holonVersion.STARNETDNA.Version == holon.STARNETDNA.Version)
                            continue;

                        holonVersion.STARNETDNA = newSTARNETDNA;
                        holonVersion.Name = newSTARNETDNA.Name;
                        holonVersion.Description = newSTARNETDNA.Description;
                        holonVersion.MetaData["STARNETHolonName"] = newSTARNETDNA.Name;

                        oldPath = holonVersion.STARNETDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARNETDNA.Name);
                        holonVersion.STARNETDNA.SourcePath = newPath;
                        holonVersion.STARNETDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(holonVersion.STARNETDNA.PublishedPath))
                        {
                            oldPublishedPath = holonVersion.STARNETDNA.PublishedPath;
                            //holonVersion.STARNETDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).FullName, newSTARNETDNA.Name);
                            newSTARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            try
                            {
                                if (Directory.Exists(oldPath))
                                    Directory.Move(oldPath, newPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath))
                                    File.Move(oldPublishedPath, holonVersion.STARNETDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T1> templateSaveResult = await UpdateAsync(avatarId, holonVersion, false, providerType: providerType);

                        if (templateSaveResult != null && templateSaveResult.Result != null && !templateSaveResult.IsError)
                        {

                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured calling UpdateAsync updating the STARNETDNA for {STARNETHolonUIName} with Id {holonVersion.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {templateSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for all {STARNETHolonUIName} versions caused by an error in LoadVersionsAsync. Reason: {holonsResult.Message}");


                OASISResult<IEnumerable<T3>> installedTemplatesResult = await ListInstalledAsync(avatarId, providerType);

                if (installedTemplatesResult != null && installedTemplatesResult.Result != null && !installedTemplatesResult.IsError)
                {
                    foreach (T3 installedHolon in installedTemplatesResult.Result)
                    {
                        installedHolon.STARNETDNA = newSTARNETDNA;
                        installedHolon.Name = installedHolon.Name.Replace(oldName, newSTARNETDNA.Name);
                        installedHolon.Description = installedHolon.Description.Replace(oldName, newSTARNETDNA.Name);
                        installedHolon.MetaData[STARNETHolonNameName] = newSTARNETDNA.Name;

                        oldPath = installedHolon.STARNETDNA.SourcePath;
                        newPath = Path.Combine(new DirectoryInfo(oldPath).Parent.FullName, newSTARNETDNA.Name);
                        installedHolon.STARNETDNA.SourcePath = newPath;
                        installedHolon.STARNETDNA.LaunchTarget = launchTarget;

                        if (!string.IsNullOrEmpty(installedHolon.STARNETDNA.PublishedPath))
                        {
                            oldPublishedPath = installedHolon.STARNETDNA.PublishedPath;
                            installedHolon.STARNETDNA.PublishedPath = Path.Combine(new DirectoryInfo(oldPublishedPath).Parent.FullName, string.Concat(newSTARNETDNA.Name, "_v", installedHolon.STARNETDNA.Version, ".", STARNETHolonFileExtention));
                            //holonVersion.STARNETDNA.PublishedPath = oldPublishedPath.Replace(oldName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.DownloadedPath))
                        {
                            oldDownloadedPath = installedHolon.DownloadedPath;
                            //holonVersion.DownloadedPath = Path.Combine(new DirectoryInfo(oldDownloadedPath).FullName, newSTARNETDNA.Name);
                            installedHolon.DownloadedPath = oldDownloadedPath.Replace(oldName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(installedHolon.InstalledPath))
                        {
                            oldInstalledPath = installedHolon.InstalledPath;
                            installedHolon.InstalledPath = Path.Combine(new DirectoryInfo(oldInstalledPath).Parent.FullName, newSTARNETDNA.Name);
                        }

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            try
                            {
                                if (Directory.Exists(oldPath) && oldPath != newPath)
                                    Directory.Move(oldPath, newPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} folder from {oldPath} to {newPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        if (!string.IsNullOrEmpty(oldPublishedPath))
                        {
                            try
                            {
                                if (File.Exists(oldPublishedPath) && oldPublishedPath != installedHolon.STARNETDNA.PublishedPath)
                                    File.Move(oldPublishedPath, installedHolon.STARNETDNA.PublishedPath);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} published file from {oldPublishedPath} to {newSTARNETDNA.PublishedPath}. Reason: {e}.");
                                CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                            }
                        }

                        OASISResult<T3> installedOPPSystemHolonSaveResult = await UpdateAsync(avatarId, installedHolon, providerType: providerType);

                        if (installedOPPSystemHolonSaveResult != null && installedOPPSystemHolonSaveResult.Result != null && !installedOPPSystemHolonSaveResult.IsError)
                        {
                            if (!string.IsNullOrEmpty(oldDownloadedPath))
                            {
                                try
                                {
                                    if (File.Exists(oldDownloadedPath))
                                        File.Move(oldDownloadedPath, installedHolon.DownloadedPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} downloaded file from {oldDownloadedPath} to {installedHolon.DownloadedPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }

                            if (!string.IsNullOrEmpty(oldInstalledPath))
                            {
                                try
                                {
                                    if (Directory.Exists(oldInstalledPath))
                                        Directory.Move(oldInstalledPath, installedHolon.InstalledPath);
                                }
                                catch (Exception e)
                                {
                                    OASISErrorHandling.HandleWarning(ref result, $"An error occured attempting to rename the {STARNETHolonUIName} installed folder from {oldInstalledPath} to {installedHolon.InstalledPath}. Reason: {e}.");
                                    CLIEngine.ShowErrorMessage("PLEASE RENAME THIS FOLDER MANUALLY, THANK YOU!");
                                }
                            }
                        }
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for Installed {STARNETHolonUIName} with Id {installedHolon.Id} for provider {Enum.GetName(typeof(ProviderType), providerType)}. Reason: {installedOPPSystemHolonSaveResult.Message}");
                    }
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured updating the STARNETDNA for all Installed {STARNETHolonUIName} versions caused by an error in ListInstalledSTARNETHolonsAsync. Reason: {holonsResult.Message}");


                result.Result = saveResult.Result;
                result.IsSaved = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with Id {newSTARNETDNA.Id} from the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveResult.Message}");

            return result;
        }

    }
}