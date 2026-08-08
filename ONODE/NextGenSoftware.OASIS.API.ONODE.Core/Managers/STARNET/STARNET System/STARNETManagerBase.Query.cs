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
        public virtual async Task<OASISResult<IEnumerable<T3>>> ListInstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListInstalledAsync. Reason: Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListInstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn == DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListInstalled. Reason: Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListUninstalledAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = await Data.LoadHolonsForParentAsync<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListUninstalledAsync. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T3>> ListUninstalled(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = Data.LoadHolonsForParent<T3>(avatarId, STARNETHolonInstalledHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.UninstalledOn != DateTime.MinValue);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETManagerBase.ListUninstalled. Reason:  Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListUnpublishedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListUnpublishedAsync. Reason: ";
            result = await Data.LoadHolonsForParentAsync<T1>(avatarId, STARNETHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARNETDNA.PublishedOn == DateTime.MinValue && x.STARNETDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParentAsync. Reason: {result.Message}");

            return result;
        }

        public OASISResult<IEnumerable<T1>> ListUnpublished(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListUnpublished. Reason: ";
            result = Data.LoadHolonsForParent<T1>(avatarId, STARNETHolonType, false, false, 0, true, false, 0, HolonType.All, 0, providerType);

            if (result != null && !result.IsError && result.Result != null)
                result.Result = result.Result.Where(x => x.STARNETDNA.PublishedOn == DateTime.MinValue && x.STARNETDNA.FileSize > 0);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonsForParent. Reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListDeactivatedAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListDeactivatedAsync. Reason: ";
            result = await Data.LoadHolonsByMetaDataAsync<T1>("Active", "0", STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public OASISResult<IEnumerable<T1>> ListDeactivated(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();
            string errorMessage = "Error occured in STARNETManagerBase.ListDeactivated. Reason: ";
            result = Data.LoadHolonsByMetaData<T1>("Active", "0", STARNETHolonType, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = true;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalledAsync. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsInstalled. Reason: ";

            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                if (installedSTARNETHolonsResult.Result != null)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T1> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T1> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T1>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<bool>> IsPublishedAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublishedAsync. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name},
                { "Version", version.ToString() },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<bool> IsPublished(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARNETManagerBase.IsPublished. Reason: ";

            OASISResult<T3> loadSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version },
                { "Active", "1" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (loadSTARNETHolonsResult != null && !loadSTARNETHolonsResult.IsError && loadSTARNETHolonsResult.Result != null)
            {
                if (loadSTARNETHolonsResult.Result.STARNETDNA.PublishedOn != DateTime.MinValue)
                    result.Result = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {loadSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            //if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError)
            {
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(installedSTARNETHolonsResult, result);
                result.Result = installedSTARNETHolonsResult.Result;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, version: versionSequence, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "Version", version }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, providerType: providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);
            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string name, bool active, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, name },
                { "VersionSequence", versionSequence.ToString() },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version},
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, Guid STARNETHolonId, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonIdName, STARNETHolonId.ToString() },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> LoadInstalledAsync(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalledAsync. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = await Data.LoadHolonByMetaDataAsync<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, 0, false, HolonType.All, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaDataAsync. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> LoadInstalled(Guid avatarId, string STARNETHolonName, string version, bool active, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "Error occured in STARNETManagerBase.LoadInstalled. Reason: ";
            OASISResult<T3> installedSTARNETHolonsResult = Data.LoadHolonByMetaData<T3>(new Dictionary<string, string>()
            {
                { STARNETHolonNameName, STARNETHolonName },
                { "Version", version },
                { "Active", active == true ? "1" : "0" }

            }, MetaKeyValuePairMatchMode.All, STARNETHolonInstalledHolonType, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (installedSTARNETHolonsResult != null && !installedSTARNETHolonsResult.IsError && installedSTARNETHolonsResult.Result != null)
                result.Result = installedSTARNETHolonsResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadHolonByMetaData. Reason: {installedSTARNETHolonsResult.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, T3 holon)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";

            if (holon != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(holon.InstalledPath))
                        Process.Start("explorer.exe", holon.InstalledPath);

                    else if (!string.IsNullOrEmpty(holon.DownloadedPath))
                        Process.Start("explorer.exe", new FileInfo(holon.DownloadedPath).DirectoryName);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured attempting to open the folder {result.Result.InstalledPath}. Reason: {e}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The {STARNETHolonUIName} is null!");

            return result;
        }

        public virtual async Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARNETHolonId, versionSequence, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARNETHolonId, versionSequence);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolon method, reason: {result.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T3>> OpenSTARNETHolonFolderAsync(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolderAsync. Reason:";
            result = await LoadInstalledAsync(avatarId, STARNETHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolonAsync method, reason: {result.Message}");

            return result;
        }

        public OASISResult<T3> OpenSTARNETHolonFolder(Guid avatarId, Guid STARNETHolonId, string version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            string errorMessage = "An error occured in STARNETManagerBase.OpenSTARNETHolonFolder. Reason:";
            result = LoadInstalled(avatarId, STARNETHolonId, version, providerType);

            if (result != null && !result.IsError && result.Result != null)
                OpenSTARNETHolonFolder(avatarId, result.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the {STARNETHolonUIName} with the LoadInstalledSTARNETHolon method, reason: {result.Message}");

            return result;
        }


        //private T4 ConvertSTARNETHolonToSTARNETDNA(ISTARNETHolon T)
        //{
        //    STARNETDNA STARNETDNA = new STARNETDNA()
        //    {
        //        CelestialBodyId = T.CelestialBodyId,
        //        //CelestialBody = T.CelestialBody,
        //        CelestialBodyName = T.CelestialBody != null ? T.CelestialBody.Name : "",
        //        CelestialBodyType = T.CelestialBody != null ? T.CelestialBody.HolonType : HolonType.None,
        //        CreatedByAvatarId = T.CreatedByAvatarId,
        //        CreatedByAvatarUsername = T.CreatedByAvatarUsername,
        //        CreatedOn = T.CreatedDate,
        //        Description = T.Description,
        //        GenesisType = T.GenesisType,
        //        STARNETHolonId = T.Id,
        //        STARNETHolonName = T.Name,
        //        STARNETHolonType = T.STARNETHolonType,
        //        PublishedByAvatarId = T.PublishedByAvatarId,
        //        PublishedByAvatarUsername = T.PublishedByAvatarUsername,
        //        PublishedOn = T.PublishedOn,
        //        PublishedOnSTARNET = T.PublishedSTARNETHolon != null,
        //        Version = T.Version.ToString()
        //    };

        //    List<IZome> zomes = new List<IZome>();
        //    foreach (IHolon holon in T.Children)
        //        zomes.Remove((IZome)holon);

        //   //STARNETDNA.Zomes = zomes;
        //    return STARNETDNA;
        //}

    }
}