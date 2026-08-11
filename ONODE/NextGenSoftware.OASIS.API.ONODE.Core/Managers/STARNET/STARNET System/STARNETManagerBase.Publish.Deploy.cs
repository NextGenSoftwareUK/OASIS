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

        public OASISResult<T1> FininalizePublish(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = Update(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = UpdateNumberOfVersionCounts(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in UnpublishAsync. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Unpublish. Reason: ";

            holon.STARNETDNA.PublishedOn = DateTime.MinValue;
            holon.STARNETDNA.PublishedByAvatarId = Guid.Empty;
            holon.STARNETDNA.PublishedByAvatarUsername = "";
            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Unpublished";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await UnpublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Unpublish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in UnpublishUnpublish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> UnpublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in UnpublishSTARNETHolonAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await UnpublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Unpublish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Unpublish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Unpublish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the Load method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the UpdateAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Republish. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                holon.STARNETDNA.PublishedOn = DateTime.Now;
                holon.STARNETDNA.PublishedByAvatarId = avatarId;
                holon.STARNETDNA.PublishedByAvatarUsername = avatarResult.Result.Username;
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Republished";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in RepublishAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await RepublishAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Republish. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Republish(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> RepublishAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await RepublishAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in RepublishAsync loading the {STARNETHolonUIName} with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Republish(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Republish(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Republish loading the {STARNETHolonUIName} with the Load method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Deactivate. Reason: ";

            //T.STARNETDNA.IsActive = false;
            holon.MetaData["Active"] = "0";

            OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
            {
                result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                result.Message = $"{STARNETHolonUIName} Deactivated";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = await LoadAsync(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await DeactivateAsync(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeactivateAsync loading the T with the LoadAsync method, reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, Guid STARNETHolonId, int version, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> loadResult = Load(STARNETHolonId, avatarId, version, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = Deactivate(avatarId, loadResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Deactivate loading the T with the LoadSTARNETHolon method, reason: {loadResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> DeactivateAsync(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = await LoadAsync(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in DeactivateAsync. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = await DeactivateAsync(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolonAsync method, reason: {oappResult.Message}");

            return result;
        }

        public OASISResult<T1> Deactivate(Guid avatarId, T4 STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            OASISResult<T1> oappResult = Load(STARNETDNA.Id, avatarId, STARNETDNA.VersionSequence, providerType: providerType);
            string errorMessage = "Error occured in Deactivate. Reason: ";

            if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                result = Deactivate(avatarId, oappResult.Result, providerType);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the {STARNETHolonUIName} with the LoadSTARNETHolon method, reason: {oappResult.Message}");

            return result;
        }

        public virtual async Task<OASISResult<T1>> ActivateAsync(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ActivateAsync. Reason: ";

            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = await UpdateAsync(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the SaveSTARNETHolonAsync method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<T1> Activate(Guid avatarId, T1 holon, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in Activate. Reason: ";

            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
            {
                //T.STARNETDNA.IsActive = true;
                holon.MetaData["Active"] = "1";

                OASISResult<T1> oappResult = Update(avatarId, holon, providerType: providerType);

                if (oappResult != null && oappResult.Result != null && !oappResult.IsError)
                {
                    result.Result = oappResult.Result; //ConvertSTARNETHolonToSTARNETDNA(T);
                    result.Message = $"{STARNETHolonUIName} Activated";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving the {STARNETHolonUIName} with the Update method, reason: {oappResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the Avatar with the LoadAvatar method, reason: {avatarResult.Message}");

            return result;
        }

    }
}
