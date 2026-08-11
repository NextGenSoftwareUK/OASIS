using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class ProviderManager
    {
        public bool SetAutoReplicationForProviders(bool autoReplicate, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(autoReplicate, providers, _providersThatAreAutoReplicating);
        }

        public OASISResult<bool> SetAutoReplicationForProviders(bool autoReplicate, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoReplicate", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoReplicationForProviders(autoReplicate, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoReplicationListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoReplicate", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providersThatAreAutoReplicating.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providersThatAreAutoReplicating.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoReplicationListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providersThatAreAutoReplicating = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public bool SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return SetAutoReplicationForProviders(autoReplicate, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForProviders(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverList);
        }

        public bool SetAutoFailOverForProvidersForAvatarLogin(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForAvatarLogin);
        }

        public bool SetAutoFailOverForProvidersForCheckIfEmailAlreadyInUse(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfEmailAlreadyInUse);
        }

        public bool SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfUsernameAlreadyInUse);
        }

        public bool SetAutoFailOverForProvidersForCheckIfOASISSystemAccountExists(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfOASISSystemAccountExists);
        }

        public bool SetAutoFailOverLocalForProviders(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverLocalList);
        }

        public OASISResult<bool> SetAutoFailOverForProviders(bool addToFailOverList, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOver", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoFailOverForProviders(addToFailOverList, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOver", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverList.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverList.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForAvatarLogin", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForAvatarLogin.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForAvatarLogin.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfEmailAlreadyInUse(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForCheckIfEmailAlreadyInUse", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForCheckIfEmailAlreadyInUse.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForCheckIfEmailAlreadyInUse.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfUsernameAlreadyInUse(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForCheckIfUsernameAlreadyInUse", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForCheckIfUsernameAlreadyInUse.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForCheckIfUsernameAlreadyInUse.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverList = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForAvatarLogin = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfEmailAlreadyInUse(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForCheckIfEmailAlreadyInUse = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfUsernameAlreadyInUse(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForCheckIfUsernameAlreadyInUse = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverLocalListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverLocal", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverLocalList.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverLocalList.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverLocalListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverLocalList = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<T> ValidateProviderList<T>(string listName, string providerList)
        {
            string[] providers = providerList.Split(',');
            object providerTypeObject = null;

            foreach (string provider in providers)
            {
                if (!Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    return new OASISResult<T>() { Message = $"The ProviderType {provider.Trim()} passed in for the {listName} list is invalid. It must be one of the following types: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}.", IsError = true };
            }

            return new OASISResult<T>();
        }

        public OASISResult<IEnumerable<ProviderType>> GetProvidersFromList(string listName, string providerList)
        {
            OASISResult<IEnumerable<ProviderType>> result = new OASISResult<IEnumerable<ProviderType>>();
            List<ProviderType> providerTypes = new List<ProviderType>();
            string[] providers = providerList.Split(",");
            object providerTypeObject = null;
            List<string> invalidProviderTypes = new List<string>();

            foreach (string provider in providers)
            {
                if (Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    providerTypes.Add((ProviderType)providerTypeObject);
                else
                {
                    invalidProviderTypes.Add(provider.Trim());
                    //OASISErrorHandling.HandleWarning(ref result, $"{provider.Trim()} listName} list is invalid.");
                    OASISErrorHandling.HandleWarning(ref result, $"Error in GetProvidersFromList method in ProviderManager, the provider {provider.Trim()} specified in the {listName} list is invalid.");
                }
            }

            if (result.WarningCount > 0)
                result.Message = $"Error in GetProvidersFromList method in ProviderManager. {result.WarningCount} provider type(s) passed in for the {listName} list are invalid:\n\n{OASISResultHelper.BuildInnerMessageError(invalidProviderTypes, ", ", true)}.\n\nThey must be one of the following values: {EnumHelper.GetEnumValues(typeof(ProviderType))}";
            //result.Message = $"Error in GetProvidersFromList method in ProviderManager. {result.WarningCount} provider type(s) passed in for the {listName} are invalid:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}.\n\nThey must be one of the following values: {EnumHelper.GetEnumValues(typeof(ProviderType))}";

            result.Result = providerTypes;
            return result;
        }

        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersFromListAsEnumList(string listName, string providerList)
        {
            OASISResult<IEnumerable<EnumValue<ProviderType>>> result = new OASISResult<IEnumerable<EnumValue<ProviderType>>>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList(listName, providerList);

            if (!listResult.IsError && listResult.Result != null)
                result.Result = EnumHelper.ConvertToEnumValueList(listResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProvidersFromListAsEnumList method in ProviderManager. Reason: {listResult.Message}", listResult.DetailedMessage);

            return result;
        }

        public bool SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return SetAutoFailOverForProviders(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForAllProvidersForAvatarLogin(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForAvatarLogin(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForAllProvidersForCheckIfEmailAlreadyInUse(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForCheckIfEmailAlreadyInUse(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoLoadBalanceForProviders(bool addToLoadBalanceList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToLoadBalanceList, providers, _providerAutoLoadBalanceList);
        }

        public OASISResult<bool> SetAutoLoadBalanceForProviders(bool addToLoadBalanceList, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoLoadBalance", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoLoadBalanceForProviders(addToLoadBalanceList, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoLoadBalanceListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoLoadBalance", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            if (!listResult.IsError && listResult.Result != null)
            {
                _providerAutoLoadBalanceList.Clear();
                foreach (ProviderType providerType in listResult.Result)
                    _providerAutoLoadBalanceList.Add(new EnumValue<ProviderType>(providerType));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in SetAndReplaceAutoLoadBalanceListForProviders method in ProviderManager. Reason: {listResult.Result}");

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoLoadBalanceListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoLoadBalanceList = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public bool SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return SetAutoLoadBalanceForProviders(addToLoadBalanceList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public List<EnumValue<ProviderType>> GetProviderAutoLoadBalanceList()
        {
            return _providerAutoLoadBalanceList;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverList()
        {
            return _providerAutoFailOverList;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForAvatarLogin()
        {
            return _providerAutoFailOverListForAvatarLogin;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse()
        {
            return _providerAutoFailOverListForCheckIfEmailAlreadyInUse;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse()
        {
            return _providerAutoFailOverListForCheckIfUsernameAlreadyInUse;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfOASISSystemAccountExists()
        {
            return _providerAutoFailOverListForCheckIfOASISSystemAccountExists;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverLocalList()
        {
            return _providerAutoFailOverLocalList;
        }

        /// <summary>Try each entry in <see cref="GetProviderAutoFailOverLocalList"/> after the current storage provider until one activates successfully. Used when connectivity to remote providers is lost and the host should stay on local-capable storage only.</summary>
        public OASISResult<IOASISStorageProvider> ActivateNextLocalAutoFailOverStorageProvider()
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();
            string errorMessage = "Error Occured In ProviderManager.ActivateNextLocalAutoFailOverStorageProvider. Reason: ";

            if (!IsAutoFailOverLocalProvidersEnabled)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}IsAutoFailOverLocalProvidersEnabled is false.");
                return result;
            }

            if (_providerAutoFailOverLocalList == null || _providerAutoFailOverLocalList.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}AutoFailOverLocalProviders list is empty.");
                return result;
            }

            ProviderType current = CurrentStorageProviderType.Value;
            int startIndex = 0;
            for (int i = 0; i < _providerAutoFailOverLocalList.Count; i++)
            {
                if (_providerAutoFailOverLocalList[i].Value == current)
                {
                    startIndex = i + 1;
                    break;
                }
            }

            for (int step = 0; step < _providerAutoFailOverLocalList.Count; step++)
            {
                int idx = (startIndex + step) % _providerAutoFailOverLocalList.Count;
                ProviderType nextType = _providerAutoFailOverLocalList[idx].Value;
                OASISResult<IOASISStorageProvider> activateResult = SetAndActivateCurrentStorageProvider(nextType);
                if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    return activateResult;
            }

            OASISErrorHandling.HandleError(ref result, $"{errorMessage}No provider in AutoFailOverLocalProviders could be activated.");
            return result;
        }

    }
}
