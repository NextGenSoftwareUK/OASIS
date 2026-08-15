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
    }
}
