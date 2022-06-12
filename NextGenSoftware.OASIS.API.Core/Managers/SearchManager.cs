using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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

        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, ProviderType providerType = ProviderType.Default, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await SearchProviderAsync(searchParams, result, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
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

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to search for search term ", searchParams.SearchQuery, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The search term ", searchParams.SearchQuery, " completed successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to complete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Search Completed Successfully.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured searching for ", searchParams.SearchQuery, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        private async Task<OASISResult<ISearchResults>> SearchProviderAsync(ISearchParams searchParams, OASISResult<ISearchResults> result, ProviderType providerType = ProviderType.Default, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            string errorMessageTemplate = "Error in SearchProviderAsync method in OASISManager for {0} for provider {1}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, searchParams.SearchQuery, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, searchParams.SearchQuery, ProviderManager.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "Unknown Error";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.IsLoaded = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }
    }
}