using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories
{
    public class GenericRepository
    {
        public IHoloNETClientAppAgent HoloNETClientAppAgent { get; private set; }
        public bool UseReflection { get; private set; }

        public GenericRepository(IHoloNETClientAppAgent HoloNETClientAppAgent, bool useReflection)
        {
            this.HoloNETClientAppAgent = HoloNETClientAppAgent;
            this.UseReflection = useReflection;
        }

        public async Task<OASISResult<T>> LoadAsync<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    result = HandleLoadResponse(await hcObject.LoadByCustomFieldAsync(fieldValue, fieldName, customDataKeyValuePairs, UseReflection), hcObjectType, fieldName, fieldValue, hcObject, result, "LoadAsync");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with fieldName {fieldName} and fieldValue {fieldValue} in the LoadAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<T> Load<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    result = HandleLoadResponse(hcObject.LoadByCustomField(fieldValue, fieldName, version, customDataKeyValuePairs, UseReflection), hcObjectType, fieldName, fieldValue, hcObject, result, "Load");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with fieldName {fieldName} and fieldValue {fieldValue} in the Load method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(HcObjectTypeEnum hcObjectType, T holon, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                ZomeFunctionCallBackEventArgs response = null;
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!UseReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = await hcObject.SaveAsync(DataHelper.ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = await hcObject.SaveAsync(DataHelper.ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;

                        case HcObjectTypeEnum.Holon:
                            response = await hcObject.SaveAsync(DataHelper.ConvertHolonToParamsObject((IHolon)holon, customDataKeyValuePairs));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = DataHelper.ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = DataHelper.ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;

                        case HcObjectTypeEnum.Holon:
                            hcObject = DataHelper.ConvertHolonToHoloOASISHolon((IHolon)holon, (IHcHolon)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = await hcObject.SaveAsync(customDataKeyValuePairs);
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result, "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the SaveAsync method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T> Save<T>(HcObjectTypeEnum hcObjectType, T holon, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;
                ZomeFunctionCallBackEventArgs response = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!UseReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = hcObject.Save(DataHelper.ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = hcObject.Save(DataHelper.ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;

                        case HcObjectTypeEnum.Holon:
                            response = hcObject.Save(DataHelper.ConvertHolonToParamsObject((IHolon)holon, customDataKeyValuePairs));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = DataHelper.ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = DataHelper.ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;

                        case HcObjectTypeEnum.Holon:
                            hcObject = DataHelper.ConvertHolonToHoloOASISHolon((IHolon)holon, (IHcHolon)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = hcObject.Save(customDataKeyValuePairs);
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result, "Save");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the Save method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> DeleteAsync(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string errorMessage = $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the DeleteAsync method in HoloOASIS Provider. Reason:";

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcObject.ZomeDeleteEntryFunction = zomeDeleteFunctionName;

                ZomeFunctionCallBackEventArgs response = hcObject.DeleteByCustomField(fieldValue, fieldName, customDataKeyValuePairs);

                if (response != null)
                    result = HandleDeleteResponse(response, hcObjectType, fieldName, fieldValue, hcObject, result, "DeleteAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            return result;
        }

        public OASISResult<IHolon> Delete(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcObject.ZomeDeleteEntryFunction = zomeDeleteFunctionName;

                ZomeFunctionCallBackEventArgs response = hcObject.DeleteByCustomField(fieldValue, fieldName, customDataKeyValuePairs);

                if (response != null)
                    result = HandleDeleteResponse(response, hcObjectType, fieldName, fieldValue, hcObject, result, "Delete");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the Delete method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<T> HandleLoadResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHoloNETAuditEntryBase hcObject, OASISResult<T> result, string methodName) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = DataHelper.ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: {response.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleSaveResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, IHolonBase holon, IHoloNETAuditEntryBase hcObject, OASISResult<T> result, string methodName) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = DataHelper.ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: {response.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleDeleteResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHoloNETAuditEntryBase hcObject, OASISResult<T> result, string methodName) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = DataHelper.ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: {response.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the GenericRepository in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }
    }
}
