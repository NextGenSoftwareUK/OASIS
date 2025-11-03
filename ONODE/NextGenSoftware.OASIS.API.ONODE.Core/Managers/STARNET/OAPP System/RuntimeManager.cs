using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;
using System.Diagnostics;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class RuntimeManager : STARNETManagerBase<Runtime, DownloadedRuntime, InstalledRuntime, STARNETDNA>
    {
        public RuntimeManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(RuntimeType),
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            //"RuntimeId",
            "STARNETHolonId",
            "RuntimeName",
            "RuntimeType",
            "oruntime",
            "oasis_runtimes",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }

        public RuntimeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(RuntimeType),
            HolonType.Runtime,
            HolonType.InstalledRuntime,
            "Runtime",
            //"RuntimeId",
            "STARNETHolonId",
            "RuntimeName",
            "RuntimeType",
            "oruntime",
            "oasis_runtimes",
            "RuntimeDNA.json",
            "RuntimeDNAJSON")
        { }


        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallOASISRuntimeAsync(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, "OASIS Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallOASISRuntime(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, "OASIS Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallSTARRuntimeAsync(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, "STAR Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        public OASISResult<IInstalledRuntime> DownloadAndInstallSTARRuntime(Guid avatarId, string version, string downloadPath, string installPath, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, "STAR Runtime", version, installPath, downloadPath, providerType: providerType));
        }

        /// <summary>
        /// Starts a runtime by its ID
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="runtimeId">The runtime ID to start</param>
        /// <returns>OASISResult indicating success or failure</returns>
        public async Task<OASISResult<bool>> StartAsync(Guid avatarId, Guid runtimeId)
        {
            try
            {
                // Load the installed runtime
                var runtimeResult = await LoadInstalledAsync(avatarId, runtimeId, 0);
                
                if (runtimeResult.IsError || runtimeResult.Result == null)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Runtime with ID {runtimeId} not found or not installed",
                        Result = false
                    };
                }

                var runtime = runtimeResult.Result;
                
                // Check if runtime is already running
                if (runtime.IsRunning)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = $"Runtime {runtime.Name} is already running",
                        Result = true
                    };
                }

                Process.Start(runtime.StartCommand);

                return new OASISResult<bool>
                {
                    IsError = false,
                    Message = $"Runtime {runtime.Name} started successfully",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error starting runtime: {ex.Message}",
                    Exception = ex,
                    Result = false
                };
            }
        }

        /// <summary>
        /// Stops a runtime by its ID
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="runtimeId">The runtime ID to stop</param>
        /// <returns>OASISResult indicating success or failure</returns>
        public async Task<OASISResult<bool>> StopAsync(Guid avatarId, Guid runtimeId)
        {
            try
            {
                // Load the installed runtime
                var runtimeResult = await LoadInstalledAsync(avatarId, runtimeId, 0);
                
                if (runtimeResult.IsError || runtimeResult.Result == null)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Runtime with ID {runtimeId} not found or not installed",
                        Result = false
                    };
                }

                var runtime = runtimeResult.Result;
                
                // Check if runtime is already stopped
                if (!runtime.IsRunning)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = $"Runtime {runtime.Name} is already stopped",
                        Result = true
                    };
                }

                Process.Start(runtime.StopCommand);

                return new OASISResult<bool>
                {
                    IsError = false,
                    Message = $"Runtime {runtime.Name} stopped successfully",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error stopping runtime: {ex.Message}",
                    Exception = ex,
                    Result = false
                };
            }
        }

        /// <summary>
        /// Gets the status of a runtime by its ID
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="runtimeId">The runtime ID to check</param>
        /// <returns>OASISResult with runtime status information</returns>
        public async Task<OASISResult<object>> GetStatusAsync(Guid avatarId, Guid runtimeId)
        {
            try
            {
                // Load the installed runtime
                var runtimeResult = await LoadInstalledAsync(avatarId, runtimeId, 0);
                
                if (runtimeResult.IsError || runtimeResult.Result == null)
                {
                    return new OASISResult<object>
                    {
                        IsError = true,
                        Message = $"Runtime with ID {runtimeId} not found or not installed",
                        Result = null
                    };
                }

                var runtime = runtimeResult.Result;
                
                var status = new
                {
                    RuntimeId = runtimeId,
                    RuntimeName = runtime.Name,
                    IsRunning = runtime.IsRunning,
                    Status = runtime.IsRunning ? "Running" : "Stopped",
                    Version = runtime.STARNETDNA.Version,
                    InstalledPath = runtime.InstalledPath,
                    LastModified = runtime.ModifiedDate,
                    Created = runtime.CreatedDate
                };

                return new OASISResult<object>
                {
                    IsError = false,
                    Message = $"Runtime status retrieved successfully",
                    Result = status
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error getting runtime status: {ex.Message}",
                    Exception = ex,
                    Result = null
                };
            }
        }

        private OASISResult<IInstalledRuntime> ProcessResult(OASISResult<InstalledRuntime> operationResult)
        {
            OASISResult<IInstalledRuntime> result = new OASISResult<IInstalledRuntime>();
            result.Result = (IInstalledRuntime)operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}