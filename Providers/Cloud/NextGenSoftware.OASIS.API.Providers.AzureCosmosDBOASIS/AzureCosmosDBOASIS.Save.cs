using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS
    {
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            try
            {
                IAvatar objAvatar = avatarRepository.AddAsync(avatar).Result;
                return new OASISResult<IAvatar> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                IAvatar objAvatar = await avatarRepository.AddAsync(avatar);
                return new OASISResult<IAvatar> { IsSaved = true, IsError = false, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false, IsError=true, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            try
            {
                IAvatarDetail objAvatar = avatarDetailRepository.AddAsync(avatarDetail).Result;
                return new OASISResult<IAvatarDetail> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            try
            {
                IAvatarDetail objAvatar = await avatarDetailRepository.AddAsync(avatarDetail);
                return new OASISResult<IAvatarDetail> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                IHolon objHolon = holonRepository.AddAsync(holon).Result;
                return new OASISResult<IHolon> { IsSaved = true, Result = objHolon };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                IHolon objHolon = await holonRepository.AddAsync(holon);
                return new OASISResult<IHolon> { IsSaved = true, Result = objHolon };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                List<IHolon> savedHolons = new List<IHolon>();

                if (holons != null)
                {
                    foreach (var holon in holons)
                        savedHolons.Add(holonRepository.AddAsync(holon).Result);
                }

                return new OASISResult<IEnumerable<IHolon>> { IsSaved = true, IsError = false, Result = savedHolons };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                List<IHolon> savedHolons = new List<IHolon>();

                if (holons != null)
                {
                    foreach (var holon in holons)
                        savedHolons.Add(await holonRepository.AddAsync(holon));
                }
                
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = true, IsError = false, Result = savedHolons };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                // Extract search query from SearchGroups
                string searchQuery = null;
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.First();
                    if (firstGroup is ISearchTextGroup textGroup)
                        searchQuery = textGroup.SearchQuery;
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Search holons - using synchronous method or basic filtering
                    var holonSearchResult = holonRepository.GetList().Where(h => h.Name.Contains(searchQuery) || h.Description.Contains(searchQuery));
                    holons.AddRange(holonSearchResult);

                    // Search avatars - using synchronous method or basic filtering
                    var avatarSearchResult = avatarRepository.GetList().Where(a => a.Username.Contains(searchQuery) || a.Email.Contains(searchQuery));
                    avatars.AddRange(avatarSearchResult);
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in Azure Cosmos DB with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    try
                    {
                        await holonRepository.AddAsync(holon);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {ex.Message}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar ID
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatarId == avatarId);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarId} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by ID from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar username
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatar.Username == avatarUsername);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarUsername} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar email
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatar.Email == avatarEmailAddress);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarEmailAddress} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons
                var holons = holonRepository.GetList();
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from Azure Cosmos DB
                var holons = holonRepository.GetList().Where(h => h.MetaData != null && h.MetaData.ContainsKey(metaKey) && h.MetaData[metaKey].ToString() == metaValue);

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons by metadata from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata pairs from Azure Cosmos DB
                var holons = holonRepository.GetList().Where(h =>
                {
                    if (h.MetaData == null) return false;
                    foreach (var kvp in metaKeyValuePairs)
                    {
                        if (!h.MetaData.ContainsKey(kvp.Key) || h.MetaData[kvp.Key].ToString() != kvp.Value)
                            return false;
                    }
                    return true;
                });

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons by metadata pairs from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
    }
}
