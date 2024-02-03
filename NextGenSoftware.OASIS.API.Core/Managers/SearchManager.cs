using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class SearchManager : OASISManager
    {
        private static SearchManager _instance = null;

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static SearchManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SearchManager(ProviderManager.CurrentStorageProvider);

                return _instance;
            }
        }

        public SearchManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        //public async Task<OASISResult<ISearchResults>> BasicSearchAsync(ISearchTextGroup searchParams, ProviderType providerType = ProviderType.Default, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{

        //}

        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, ProviderType providerType = ProviderType.Default, List<ProviderType> additionalProvidersToSearch = null, bool allowDuplicates = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;
            
            try
            {
                if (providerType == ProviderType.All)
                    await SearchListOfProvidersAsync(searchParams, result, Enum.GetValues(typeof(ProviderType)).OfType<ProviderType>().ToList(), allowDuplicates, loadChildren, recursive, maxChildDepth, continueOnError, version);

                else if (additionalProvidersToSearch != null)
                {
                    result = await SearchProviderAsync(searchParams, result, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    await SearchListOfProvidersAsync(searchParams, result, additionalProvidersToSearch, allowDuplicates, loadChildren, recursive, maxChildDepth, continueOnError, version);
                }
                else
                {
                    result = await SearchProviderAsync(searchParams, result, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                    if ((result.Result == null || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                        {
                            if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            {
                                result = await SearchProviderAsync(searchParams, result, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version);

                                if (!result.IsError && result.Result != null)
                                    break;
                            }
                        }
                    }

                    if (result.Result == null || result.IsError)
                        OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to search. ErrorCount: ", result.ErrorCount, ". WarningCount: ", result.WarningCount, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                    {
                        result.IsLoaded = true;

                        if (result.WarningCount > 0)
                            OASISErrorHandling.HandleWarning(ref result, string.Concat("The search completed successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to complete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ". ErrorCount: ", result.ErrorCount, ".WarningCount: ", result.WarningCount, "."), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                            result.Message = "Search Completed Successfully.";

                        result.Result = FilterResults(result.Result.SearchResultAvatars, result.Result.SearchResultHolons, allowDuplicates);
                    }

                    // Set the current provider back to the original provider.
                    ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured searchin in provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        private ISearchResults FilterResults(List<IAvatar> avatars, List<IHolon> holons, bool allowDuplicates = true)
        {
            ISearchResults results = new SearchResults();

            foreach (IAvatar avatar in avatars)
            {
                if (results.SearchResultAvatars.Contains(avatar))
                {
                    results.NumberOfDuplicates++;

                    if (allowDuplicates)
                        results.SearchResultAvatars.Add(avatar);
                }
                else
                    results.SearchResultAvatars.Add(avatar);

                results.NumberOfResults++;
            }

            foreach (IHolon holon in holons)
            {
                if (results.SearchResultHolons.Contains(holon))
                {
                    results.NumberOfDuplicates++;

                    if (allowDuplicates)
                        results.SearchResultHolons.Add(holon);
                }
                else
                    results.SearchResultHolons.Add(holon);

                results.NumberOfResults++;
            }
            
            return results;
        }

        private async Task<OASISResult<ISearchResults>> SearchListOfProvidersAsync(ISearchParams searchParams, OASISResult<ISearchResults> result, List<ProviderType> providerTypes, bool allowDuplicates = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            List<IAvatar> avatars = new List<IAvatar>();
            List<IHolon> holons = new List<IHolon>();

            if (result != null && !result.IsError && result.Result != null)
            {
                avatars.AddRange(result.Result.SearchResultAvatars);
                holons.AddRange(result.Result.SearchResultHolons);
            }

            foreach (ProviderType type in providerTypes)
            {
                result = await SearchProviderAsync(searchParams, result, type, loadChildren, recursive, maxChildDepth, continueOnError, version);

                if (!result.IsError && result.Result != null)
                {
                    avatars.AddRange(result.Result.SearchResultAvatars);
                    holons.AddRange(result.Result.SearchResultHolons);
                }
            }

            result.Result = FilterResults(avatars, holons, allowDuplicates);

            if (result.ErrorCount > 0 || result.WarningCount > 0)
                OASISErrorHandling.HandleError(ref result, String.Concat("One ore more OASIS Providers failed to search. ErrorCount: ", result.ErrorCount, ". WarningCount: ", result.WarningCount, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));

            return result;
        }

        private async Task<OASISResult<ISearchResults>> SearchProviderAsync(ISearchParams searchParams, OASISResult<ISearchResults> result, ProviderType providerType = ProviderType.Default, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            string errorMessageTemplate = "Error in SearchProviderAsync method in SearchManager for provider {1}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "Unknown Error";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.IsLoaded = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }
    }
}