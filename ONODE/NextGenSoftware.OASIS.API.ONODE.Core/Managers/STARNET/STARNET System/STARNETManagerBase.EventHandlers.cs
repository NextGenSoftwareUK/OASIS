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
        private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.NotStarted:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.NotStarted });
                    break;

                case Google.Apis.Upload.UploadStatus.Starting:
                    _progress = 0;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                    break;

                case Google.Apis.Upload.UploadStatus.Completed:
                    _progress = 100;
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploaded });
                    break;

                case Google.Apis.Upload.UploadStatus.Uploading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesSent / (double)_fileLength * 100);
                            OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                        }
                    }
                    break;

                case Google.Apis.Upload.UploadStatus.Failed:
                    OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void OnDownloadProgress(Google.Apis.Download.IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case Google.Apis.Download.DownloadStatus.NotStarted:
                    _progress = 0;
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.NotStarted });
                    break;

                case Google.Apis.Download.DownloadStatus.Completed:
                    _progress = 100;
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloaded });
                    break;

                case Google.Apis.Download.DownloadStatus.Downloading:
                    {
                        if (_fileLength > 0)
                        {
                            _progress = Convert.ToInt32(progress.BytesDownloaded / (double)_fileLength * 100);
                            // _progress = Convert.ToInt32(_fileLength / progress.BytesDownloaded);
                            OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Downloading });
                        }
                    }
                    break;

                case Google.Apis.Download.DownloadStatus.Failed:
                    OnDownloadStatusChanged?.Invoke(this, new STARNETHolonDownloadProgressEventArgs() { Progress = _progress, Status = STARNETHolonDownloadStatus.Error, ErrorMessage = progress.Exception.ToString() });
                    break;
            }
        }

        private void TemplateManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void LibManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void RuntimeManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void RuntimeManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void TemplateManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void LibManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void QuestManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void QuestManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void OAPPManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void OAPPManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void MissionManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void MissionManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void ChapterManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void ChapterManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void NFTManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void NFTManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void GeoNFTManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void GeoNFTManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void NFTCollectionManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void NFTCollectionManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void GeoNFTCollectionManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void GeoNFTCollectionManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void GeoHotSpotManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void GeoHotSpotManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void CelestialSpaceManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void CelestialSpaceManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void CelestialBodyManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void CelestialBodyManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void ZomeManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void ZomeManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void HolonManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void HolonManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void InventoryItemManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void InventoryItemManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void CelestialBodyMetaDataDNAManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void CelestialBodyMetaDataDNAManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void ZomeMetaDataDNAManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void ZomeMetaDataDNAManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        private void HolonMetaDataDNAManager_OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            OnInstallStatusChanged?.Invoke(sender, e);
        }

        private void HolonMetaDataDNAManager_OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            OnDownloadStatusChanged?.Invoke(sender, e);
        }

        #region Clone Methods

        /// <summary>
        /// Clones a STARNET holon by its ID
        /// </summary>
        /// <param name="avatarId">The avatar ID for authentication/security</param>
        /// <param name="holonId">The ID of the holon to clone</param>
        /// <param name="newName">Optional new name for the cloned holon (if not provided, will append " - Clone" to original name)</param>
        /// <param name="providerType">The provider type to use</param>
        /// <returns>OASISResult containing the cloned holon</returns>
        public async Task<OASISResult<T1>> CloneAsync(Guid avatarId, Guid holonId, string newName = null, ProviderType providerType = ProviderType.Default)
        {
            try
            {
                // Load the original holon
                var originalResult = await LoadAsync(avatarId, holonId, 0, HolonType.Default, providerType);
                
                if (originalResult.IsError || originalResult.Result == null)
                {
                    return new OASISResult<T1>
                    {
                        IsError = true,
                        Message = $"Holon with ID {holonId} not found",
                        Result = default(T1)
                    };
                }

                var originalHolon = originalResult.Result;
                
                // Create a new holon instance
                var clonedHolon = new T1();
                
                // Copy properties from original to clone
                clonedHolon.Name = string.IsNullOrEmpty(newName) ? $"{originalHolon.Name} - Clone" : newName;
                clonedHolon.Description = originalHolon.Description;
                clonedHolon.Version = 1; // Reset version for clone
                clonedHolon.CreatedByAvatarId = avatarId;
                clonedHolon.ModifiedByAvatarId = avatarId;
                clonedHolon.CreatedDate = DateTime.UtcNow;
                clonedHolon.ModifiedDate = DateTime.UtcNow;
                
                // Copy metadata if it exists
                if (originalHolon.MetaData != null)
                {
                    clonedHolon.MetaData = new Dictionary<string, object>(originalHolon.MetaData);
                    // Update metadata to indicate this is a clone
                    clonedHolon.MetaData["IsClone"] = true;
                    clonedHolon.MetaData["OriginalHolonId"] = originalHolon.Id;
                    clonedHolon.MetaData["ClonedDate"] = DateTime.UtcNow;
                    clonedHolon.MetaData["ClonedByAvatarId"] = avatarId;
                }
                else
                {
                    clonedHolon.MetaData = new Dictionary<string, object>
                    {
                        ["IsClone"] = true,
                        ["OriginalHolonId"] = originalHolon.Id,
                        ["ClonedDate"] = DateTime.UtcNow,
                        ["ClonedByAvatarId"] = avatarId
                    };
                }

                // Copy ISTARNETHolon specific properties
                if (originalHolon is ISTARNETHolon originalSTARNET && clonedHolon is ISTARNETHolon clonedSTARNET)
                {
                    // Clone STARNETDNA if it exists (copy the entire object)
                    if (originalSTARNET.STARNETDNA != null)
                    {
                        clonedSTARNET.STARNETDNA = originalSTARNET.STARNETDNA;
                    }
                    
                    // Clone PublishedSTARNETHolon if it exists
                    if (originalSTARNET.PublishedSTARNETHolon != null)
                    {
                        clonedSTARNET.PublishedSTARNETHolon = new byte[originalSTARNET.PublishedSTARNETHolon.Length];
                        Array.Copy(originalSTARNET.PublishedSTARNETHolon, clonedSTARNET.PublishedSTARNETHolon, originalSTARNET.PublishedSTARNETHolon.Length);
                    }
                }

                // Save the cloned holon
                var saveResult = await Data.SaveHolonAsync<T1>(clonedHolon, avatarId, true, true, 0, true, false, providerType);
                
                if (saveResult.IsError)
                {
                    return new OASISResult<T1>
                    {
                        IsError = true,
                        Message = $"Failed to save cloned holon: {saveResult.Message}",
                        Exception = saveResult.Exception,
                        Result = default(T1)
                    };
                }

                return new OASISResult<T1>
                {
                    IsError = false,
                    Message = $"Holon '{originalHolon.Name}' cloned successfully as '{clonedHolon.Name}'",
                    Result = saveResult.Result
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<T1>
                {
                    IsError = true,
                    Message = $"Error cloning holon: {ex.Message}",
                    Exception = ex,
                    Result = default(T1)
                };
            }
        }

        /// <summary>
        /// Clones a STARNET holon by its ID (synchronous version)
        /// </summary>
        /// <param name="avatarId">The avatar ID for authentication/security</param>
        /// <param name="holonId">The ID of the holon to clone</param>
        /// <param name="newName">Optional new name for the cloned holon (if not provided, will append " - Clone" to original name)</param>
        /// <param name="providerType">The provider type to use</param>
        /// <returns>OASISResult containing the cloned holon</returns>
        public OASISResult<T1> Clone(Guid avatarId, Guid holonId, string newName = null, ProviderType providerType = ProviderType.Default)
        {
            try
            {
                // Load the original holon
                var originalResult = Load(avatarId, holonId, 0, HolonType.Default, providerType);
                
                if (originalResult.IsError || originalResult.Result == null)
                {
                    return new OASISResult<T1>
                    {
                        IsError = true,
                        Message = $"Holon with ID {holonId} not found",
                        Result = default(T1)
                    };
                }

                var originalHolon = originalResult.Result;
                
                // Create a new holon instance
                var clonedHolon = new T1();
                
                // Copy properties from original to clone
                clonedHolon.Name = string.IsNullOrEmpty(newName) ? $"{originalHolon.Name} - Clone" : newName;
                clonedHolon.Description = originalHolon.Description;
                clonedHolon.Version = 1; // Reset version for clone
                clonedHolon.CreatedByAvatarId = avatarId;
                clonedHolon.ModifiedByAvatarId = avatarId;
                clonedHolon.CreatedDate = DateTime.UtcNow;
                clonedHolon.ModifiedDate = DateTime.UtcNow;
                
                // Copy metadata if it exists
                if (originalHolon.MetaData != null)
                {
                    clonedHolon.MetaData = new Dictionary<string, object>(originalHolon.MetaData);
                    // Update metadata to indicate this is a clone
                    clonedHolon.MetaData["IsClone"] = true;
                    clonedHolon.MetaData["OriginalHolonId"] = originalHolon.Id;
                    clonedHolon.MetaData["ClonedDate"] = DateTime.UtcNow;
                    clonedHolon.MetaData["ClonedByAvatarId"] = avatarId;
                }
                else
                {
                    clonedHolon.MetaData = new Dictionary<string, object>
                    {
                        ["IsClone"] = true,
                        ["OriginalHolonId"] = originalHolon.Id,
                        ["ClonedDate"] = DateTime.UtcNow,
                        ["ClonedByAvatarId"] = avatarId
                    };
                }

                // Copy ISTARNETHolon specific properties
                if (originalHolon is ISTARNETHolon originalSTARNET && clonedHolon is ISTARNETHolon clonedSTARNET)
                {
                    // Clone STARNETDNA if it exists (copy the entire object)
                    if (originalSTARNET.STARNETDNA != null)
                    {
                        clonedSTARNET.STARNETDNA = originalSTARNET.STARNETDNA;
                    }
                    
                    // Clone PublishedSTARNETHolon if it exists
                    if (originalSTARNET.PublishedSTARNETHolon != null)
                    {
                        clonedSTARNET.PublishedSTARNETHolon = new byte[originalSTARNET.PublishedSTARNETHolon.Length];
                        Array.Copy(originalSTARNET.PublishedSTARNETHolon, clonedSTARNET.PublishedSTARNETHolon, originalSTARNET.PublishedSTARNETHolon.Length);
                    }
                }

                // Save the cloned holon
                var saveResult = Data.SaveHolon<T1>(clonedHolon, avatarId, true, true, 0, true, false, providerType);
                
                if (saveResult.IsError)
                {
                    return new OASISResult<T1>
                    {
                        IsError = true,
                        Message = $"Failed to save cloned holon: {saveResult.Message}",
                        Exception = saveResult.Exception,
                        Result = default(T1)
                    };
                }

                return new OASISResult<T1>
                {
                    IsError = false,
                    Message = $"Holon '{originalHolon.Name}' cloned successfully as '{clonedHolon.Name}'",
                    Result = saveResult.Result
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<T1>
                {
                    IsError = true,
                    Message = $"Error cloning holon: {ex.Message}",
                    Exception = ex,
                    Result = default(T1)
                };
            }
        }

        #endregion

        #region Library Proxy Generation

        /// <summary>
        /// Generates a library proxy class when a library is added as a dependency to an OAPP
        /// </summary>
        private async Task GenerateLibraryProxyForOAPPAsync<T>(T1 oapp, T installedLibrary, string installPath) where T : IInstalledSTARNETHolon
        {
            try
            {
                if (oapp?.STARNETDNA?.SourcePath == null)
                    return;

                var sourcePath = oapp.STARNETDNA.SourcePath;
                if (!Directory.Exists(sourcePath))
                    return;

                // Get library metadata to determine provider type
                var libraryName = installedLibrary.Name ?? "Library";
                var libraryId = installedLibrary.Id.ToString();
                
                // Determine provider type from library file extension or metadata
                var libraryFiles = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
                var libraryFile = libraryFiles.FirstOrDefault(f => 
                    f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".so", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase));

                if (libraryFile == null)
                    return;

                var providerType = DetectInteropProviderType(libraryFile);
                
                // Generate proxy class
                var proxyGenerator = new LibraryProxyGenerator();
                var proxyResult = await proxyGenerator.SaveProxyClassToOAPPAsync(
                    sourcePath,
                    libraryId,
                    libraryName,
                    libraryFile,
                    providerType);

                if (!proxyResult.IsError)
                {
                    // Update Program.cs with library reference
                    var libraries = new List<(string LibraryName, string LibraryId, InteropProviderType ProviderType)>
                    {
                        (libraryName, libraryId, providerType)
                    };
                    
                    await proxyGenerator.UpdateOAPPProgramCsAsync(sourcePath, libraries);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the dependency addition
                Console.WriteLine($"Warning: Failed to generate library proxy: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects interop provider type from file extension
        /// </summary>
        private InteropProviderType DetectInteropProviderType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".dll" => InteropProviderType.DotNet, // Could also be native on Windows
                ".so" => InteropProviderType.NativePInvoke,
                ".dylib" => InteropProviderType.NativePInvoke,
                ".py" => InteropProviderType.Python,
                ".js" => InteropProviderType.JavaScript,
                ".wasm" => InteropProviderType.WebAssembly,
                _ => InteropProviderType.Auto
            };
        }

        #endregion
    }
}
