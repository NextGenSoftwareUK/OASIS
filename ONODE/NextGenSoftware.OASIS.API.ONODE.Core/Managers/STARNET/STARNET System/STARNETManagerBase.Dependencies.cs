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

        public OASISResult<T> InstallDependency<T>(Guid avatarId, STARNETDependency dependency, string defaultDownloadPath, string defaultInstallPath, string dependencyDisplayName, ProviderType providerType = ProviderType.Default) where T : IInstalledSTARNETHolon
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
                        OASISResult<InstalledRuntime> installResult = runtimeManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledLibrary> installResult = libManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledOAPPTemplate> installResult = templateManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledOAPP> installResult = OAPPManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledQuest> installResult = QuestManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledMission> installResult = MissionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledChapter> installResult = ChapterManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        ChapterManager.OnDownloadStatusChanged -= ChapterManager_OnDownloadStatusChanged;
                        ChapterManager.OnInstallStatusChanged -= ChapterManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        ChapterManager = null;
                    }
                    break;

                case DependencyType.NFT:
                    {
                        STARNFTManager STARNFTManager = new STARNFTManager(avatarId, STARDNA, OASISDNA);
                        STARNFTManager.OnDownloadStatusChanged += NFTManager_OnDownloadStatusChanged;
                        STARNFTManager.OnInstallStatusChanged += NFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFT> installResult = STARNFTManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARNFTManager.OnDownloadStatusChanged -= NFTManager_OnDownloadStatusChanged;
                        STARNFTManager.OnInstallStatusChanged -= NFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARNFTManager = null;
                    }
                    break;

                case DependencyType.GeoNFT:
                    {
                        STARGeoNFTManager STARGeoNFTManager = new STARGeoNFTManager(avatarId, STARDNA, OASISDNA);
                        STARGeoNFTManager.OnDownloadStatusChanged += GeoNFTManager_OnDownloadStatusChanged;
                        STARGeoNFTManager.OnInstallStatusChanged += GeoNFTManager_OnInstallStatusChanged;
                        OASISResult<InstalledGeoNFT> installResult = STARGeoNFTManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARGeoNFTManager.OnDownloadStatusChanged -= GeoNFTManager_OnDownloadStatusChanged;
                        STARGeoNFTManager.OnInstallStatusChanged -= GeoNFTManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARGeoNFTManager = null;
                    }
                    break;

                case DependencyType.NFTCollection:
                    {
                        STARNFTCollectionManager STARNFTCollectionManager = new STARNFTCollectionManager(avatarId, STARDNA, OASISDNA);
                        STARNFTCollectionManager.OnDownloadStatusChanged += NFTCollectionManager_OnDownloadStatusChanged;
                        STARNFTCollectionManager.OnInstallStatusChanged += NFTCollectionManager_OnInstallStatusChanged;
                        OASISResult<InstalledNFTCollection> installResult = STARNFTCollectionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledGeoNFTCollection> installResult = STARGeoNFTCollectionManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledGeoHotSpot> installResult = GeoHotSpotManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledCelestialSpace> installResult = CelestialSpaceManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledCelestialBody> installResult = CelestialBodyManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        CelestialBodyManager.OnDownloadStatusChanged -= CelestialBodyManager_OnDownloadStatusChanged;
                        CelestialBodyManager.OnInstallStatusChanged -= CelestialBodyManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        CelestialBodyManager = null;
                    }
                    break;

                case DependencyType.Zome:
                    {
                        STARZomeManager STARZomeManager = new STARZomeManager(avatarId, STARDNA, OASISDNA);
                        STARZomeManager.OnDownloadStatusChanged += ZomeManager_OnDownloadStatusChanged;
                        STARZomeManager.OnInstallStatusChanged += ZomeManager_OnInstallStatusChanged;
                        OASISResult<InstalledZome> installResult = STARZomeManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARZomeManager.OnDownloadStatusChanged -= ZomeManager_OnDownloadStatusChanged;
                        STARZomeManager.OnInstallStatusChanged -= ZomeManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARZomeManager = null;
                    }
                    break;

                case DependencyType.Holon:
                    {
                        STARHolonManager STARHolonManager = new STARHolonManager(avatarId, STARDNA, OASISDNA);
                        STARHolonManager.OnDownloadStatusChanged += HolonManager_OnDownloadStatusChanged;
                        STARHolonManager.OnInstallStatusChanged += HolonManager_OnInstallStatusChanged;
                        OASISResult<InstalledHolon> installResult = STARHolonManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
                        STARHolonManager.OnDownloadStatusChanged -= HolonManager_OnDownloadStatusChanged;
                        STARHolonManager.OnInstallStatusChanged -= HolonManager_OnInstallStatusChanged;
                        result.Result = (T)(IInstalledSTARNETHolon)installResult.Result;
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installResult, result);
                        STARHolonManager = null;
                    }
                    break;

                case DependencyType.InventoryItem:
                    {
                        InventoryItemManager InventoryItemManager = new InventoryItemManager(avatarId, STARDNA, OASISDNA);
                        InventoryItemManager.OnDownloadStatusChanged += InventoryItemManager_OnDownloadStatusChanged;
                        InventoryItemManager.OnInstallStatusChanged += InventoryItemManager_OnInstallStatusChanged;
                        OASISResult<InstalledInventoryItem> installResult = InventoryItemManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledCelestialBodyMetaDataDNA> installResult = CelestialBodyMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledZomeMetaDataDNA> installResult = ZomeMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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
                        OASISResult<InstalledHolonMetaDataDNA> installResult = HolonMetaDataDNAManager.DownloadAndInstall(avatarId, dependency.STARNETHolonId, dependency.Version, installPath, downloadPath, providerType: providerType);
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

        public bool IsThereDependencies(ISTARNETDNA STARNETDNA)
        {
            return ListAllDependencies(STARNETDNA).Count > 0;
        }

        public int GetNumberOfDependendies(ISTARNETDNA STARNETDNA)
        {
            return ListAllDependencies(STARNETDNA).Count;
        }

    }
}