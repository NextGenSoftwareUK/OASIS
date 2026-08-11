using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using System.Collections.Immutable;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {

        private void LogError(IHolon holon, ProviderType providerType, string errorMessage)
        {
            LoggingManager.Log(string.Concat("An error occured attempting to save the ", LoggingHelper.GetHolonInfoForLogging(holon), " using the ", Enum.GetName(providerType), " provider. Error Details: ", errorMessage), LogType.Error);
        }

        private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<T> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        //private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<T> holonSaveResult, string listName, string providerName) where T : IHolon
        //{
        //    holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
        //    OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
        //    result.InnerMessages.Add(holonSaveResult.Message);
        //    result.IsWarning = true;
        //    result.IsError = false;
        //    return result;
        //}

        private OASISResult<IEnumerable<T>> HandleSaveHolonForListOfProviderError<T>(OASISResult<IEnumerable<T>> result, OASISResult<IEnumerable<T>> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        private string GetSaveHolonForListOfProvidersErrorMessage(string listName, string providerName, string holoSaveResultErrorMessage)
        {
            return $"Error attempting to save in {listName} list for provider {providerName}. Reason: {holoSaveResultErrorMessage}";
        }

        //private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        //{
        //    OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);

        //    if (providerResult.IsError)
        //    {
        //        result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
        //        //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
        //        result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
        //    }
        //}

        private async Task SwitchBackToCurrentProviderAsync<T>(ProviderType currentProviderType, OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }

        private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }

        public void MapMetaData<T>(OASISResult<IEnumerable<T>> result) where T : IHolon
        {
            List<T> holons = result.Result.ToList();
            for (int i = 0; i < holons.Count(); i++)
            {
                if (holons[i].MetaData != null)
                    holons[i] = (T)MapMetaData<T>(holons[i]);
            }
        }

        public IHolon MapMetaData<T>(IHolon holon) where T : IHolon
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var type = typeof(T);
            var properties = type.GetProperties(flags);

            foreach (string key in holon.MetaData.Keys)
            {
                try
                {
                    // Case-insensitive match so MetaData keys like "objectives" (from MongoDB/JSON camelCase) match property "Objectives"
                    PropertyInfo propInfo = properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                    if (propInfo != null)
                    {
                        var underlyingGuid = Nullable.GetUnderlyingType(propInfo.PropertyType);
                        var isNullableGuid = underlyingGuid == typeof(Guid);
                        if (propInfo.PropertyType == typeof(Guid))
                        {
                            var gRaw = holon.MetaData[key];
                            if (gRaw is JsonElement jeG && jeG.ValueKind == JsonValueKind.String && Guid.TryParse(jeG.GetString(), out var gj))
                                propInfo.SetValue(holon, gj);
                            else if (gRaw is Guid gDirect)
                                propInfo.SetValue(holon, gDirect);
                            else if (gRaw != null && Guid.TryParse(gRaw.ToString(), out var gp))
                                propInfo.SetValue(holon, gp);
                        }
                        else if (isNullableGuid)
                        {
                            /* MetaData values are often JsonElement (STJ) or BSON-deserialized types; ToString() alone breaks Guid.TryParse for JsonElement. */
                            var raw = holon.MetaData[key];
                            if (raw == null)
                                propInfo.SetValue(holon, null);
                            else if (raw is JsonElement je)
                            {
                                if (je.ValueKind == JsonValueKind.Null || je.ValueKind == JsonValueKind.Undefined)
                                    propInfo.SetValue(holon, null);
                                else if (je.ValueKind == JsonValueKind.String)
                                {
                                    var js = je.GetString();
                                    if (string.IsNullOrWhiteSpace(js))
                                        propInfo.SetValue(holon, null);
                                    else if (Guid.TryParse(js, out var guidFromJe))
                                        propInfo.SetValue(holon, guidFromJe);
                                }
                            }
                            else if (raw is Guid gBox)
                                propInfo.SetValue(holon, gBox == Guid.Empty ? null : gBox);
                            else if (string.IsNullOrWhiteSpace(raw.ToString()))
                                propInfo.SetValue(holon, null);
                            else if (Guid.TryParse(raw.ToString(), out var guidVal))
                                propInfo.SetValue(holon, guidVal);
                        }
                        else if (holon.MetaData[key] != null)
                        {
                            if (propInfo.PropertyType == typeof(bool))
                                propInfo.SetValue(holon, Convert.ToBoolean(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(DateTime))
                                propInfo.SetValue(holon, Convert.ToDateTime(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(int))
                                propInfo.SetValue(holon, Convert.ToInt32(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(long))
                                propInfo.SetValue(holon, Convert.ToInt64(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(float))
                                propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key])); //TODO: Check if this is right?! :)

                            else if (propInfo.PropertyType == typeof(double))
                                propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(decimal))
                                propInfo.SetValue(holon, Convert.ToDecimal(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(UInt16))
                                propInfo.SetValue(holon, Convert.ToUInt16(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(UInt32))
                                propInfo.SetValue(holon, Convert.ToUInt32(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(UInt64))
                                propInfo.SetValue(holon, Convert.ToUInt64(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(Single))
                                propInfo.SetValue(holon, Convert.ToSingle(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(char))
                                propInfo.SetValue(holon, Convert.ToChar(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(byte))
                                propInfo.SetValue(holon, Convert.ToByte(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(sbyte))
                                propInfo.SetValue(holon, Convert.ToSByte(holon.MetaData[key]));

                            else if (propInfo.PropertyType == typeof(Color))
                                propInfo.SetValue(holon, ColorTranslator.FromHtml(holon.MetaData[key].ToString()));

                            else if (propInfo.PropertyType.IsEnum)
                                propInfo.SetValue(holon, Enum.Parse(propInfo.PropertyType, holon.MetaData[key].ToString(), true));

                            else if (propInfo.PropertyType == typeof(string))
                                propInfo.SetValue(holon, holon.MetaData[key].ToString());

                            else if (propInfo.PropertyType != typeof(string))
                            {
                                /* Complex types: MetaData may store a JSON string, or a JsonElement (array/object/string) from STJ/BSON providers. */
                                var rawMeta = holon.MetaData[key];
                                try
                                {
                                    if (rawMeta is string jsonStr)
                                    {
                                        var deserialized = JsonSerializer.Deserialize(jsonStr, propInfo.PropertyType, MetaDataComplexTypeDeserializeOptions);
                                        if (deserialized != null)
                                            propInfo.SetValue(holon, deserialized);
                                    }
                                    else if (rawMeta is JsonElement je)
                                    {
                                        if (je.ValueKind == JsonValueKind.String)
                                        {
                                            var inner = je.GetString();
                                            if (!string.IsNullOrEmpty(inner))
                                            {
                                                var deserialized = JsonSerializer.Deserialize(inner, propInfo.PropertyType, MetaDataComplexTypeDeserializeOptions);
                                                if (deserialized != null)
                                                    propInfo.SetValue(holon, deserialized);
                                            }
                                        }
                                        else if (je.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                                        {
                                            var deserialized = JsonSerializer.Deserialize(je.GetRawText(), propInfo.PropertyType, MetaDataComplexTypeDeserializeOptions);
                                            if (deserialized != null)
                                                propInfo.SetValue(holon, deserialized);
                                        }
                                    }
                                    else
                                        propInfo.SetValue(holon, rawMeta);
                                }
                                catch { /* leave property unchanged if JSON deserialize fails */ }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                //TODO: Add any other missing types...
            }

            return holon;
        }

        //private string BuildInnerMessageError(List<string> innerMessages)
        //{
        //    string result = "";
        //    foreach (string innerMessage in innerMessages)
        //        result = string.Concat(result, innerMessage, "\n\n");

        //    return result;
        //}

        private OASISResult<T> HasHolonChanged<T>(IHolon holon, ref OASISResult<T> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            if (!holon.HasHolonChanged())
            {
                result.Message = "No changes need saving";
                result.HasAnyHolonsChanged = false;
            }
            else
                result.HasAnyHolonsChanged = true;

            return result;
        }

        private OASISResult<IEnumerable<T>> HasAnyHolonsChanged<T>(IEnumerable<T> holons, ref OASISResult<IEnumerable<T>> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            foreach (IHolon holon in holons)
            {
                if (holon.HasHolonChanged())
                {
                    result.HasAnyHolonsChanged = true;
                    break;
                }
            }

            if (!result.HasAnyHolonsChanged)
                result.Message = "No changes need saving";

            return result;
        }

        private string BuildSaveHolonAutoFailOverErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("All registered OASIS Providers in the AutoFailOver List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        }

        private string BuildSaveHolonAutoReplicateErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("One or more registered OASIS Providers in the AutoReplicate List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());
        }

        private void HandleSaveHolonsErrorForAutoFailOverList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoFailOverList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonsErrorForAutoReplicateList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoReplicateList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }

    }
}
