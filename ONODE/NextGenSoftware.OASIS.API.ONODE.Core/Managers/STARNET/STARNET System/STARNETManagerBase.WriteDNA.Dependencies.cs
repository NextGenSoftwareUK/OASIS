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
    }
}
