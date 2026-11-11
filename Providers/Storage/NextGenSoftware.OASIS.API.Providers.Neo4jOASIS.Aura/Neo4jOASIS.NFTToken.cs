using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4jNode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura
{
    public partial class Neo4jOASIS
    {
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ArgumentNullException.ThrowIfNull(holon);
                var id = holon.Id == Guid.Empty ? Guid.NewGuid() : holon.Id;
                var guid = id.ToString("D");
                var providerKey = holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var suppliedKey)
                    && !string.IsNullOrWhiteSpace(suppliedKey) ? suppliedKey : guid;

                var metadata = holon.MetaData;
                var metadataJson = metadata == null ? null : OasisJson.Serialize(new { MetaData = metadata });
                var metadataTokens = metadata?.Where(pair => pair.Value != null).Select(pair =>
                    MetadataLookupToken(pair.Key, StoredRecordSearch.MetadataValueText(pair.Value))).ToArray();

                var session = Driver.AsyncSession(WithDatabase);
                await using var lifetime = session.ConfigureAwait(false);
                var storedKey = await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                        MERGE (hl:Holon {GUId: $guid})
                        SET hl.name = $name, hl.description = $description,
                            hl.ParentHolonId = $parent, hl.HolonType = $type,
                            hl.PreviousVersionId = $previous, hl.ProviderKey = coalesce(hl.ProviderKey, $providerKey),
                            hl.MetadataJson = $metadataJson, hl.MetaLookupTokens = $metadataTokens
                        RETURN hl.ProviderKey AS providerKey",
                        new { guid, name = holon.Name, description = holon.Description,
                            previous = holon.PreviousVersionId.ToString("D"), providerKey, parent = holon.ParentHolonId.ToString("D"), type = (int)holon.HolonType, metadataJson, metadataTokens }).ConfigureAwait(false);
                    return await cursor.SingleAsync(row => row["providerKey"].As<string>()).ConfigureAwait(false);
                }).ConfigureAwait(false);

                holon.Id = id;
                holon.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                holon.ProviderUniqueStorageKey[ProviderType.Value] = storedKey;
                result.Result = holon;
                result.IsSaved = true;
                result.SavedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error saving Neo4j Aura holon.", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var saved = new List<IHolon>();
            var result = new OASISResult<IEnumerable<IHolon>> { Result = saved };
            try
            {
                ArgumentNullException.ThrowIfNull(holons);
                foreach (var holon in holons)
                {
                    var item = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider)
                        .ConfigureAwait(false);
                    if (item.IsSaved && !item.IsError)
                    {
                        saved.Add(item.Result);
                        continue;
                    }

                    OASISErrorHandling.HandleError(ref result, item.Message, item.Exception);
                    if (!continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error saving Neo4j Aura holons.", ex);
            }

            result.SavedCount = saved.Count;
            result.IsSaved = !result.IsError;
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) =>
            DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) =>
            DeleteHolonBySelectorAsync("hl.GUId = $value", id.ToString("D"));

        public override OASISResult<IHolon> DeleteHolon(string providerKey) =>
            DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) =>
            DeleteHolonBySelectorAsync("hl.ProviderKey = $value", providerKey);

        private async Task<OASISResult<IHolon>> DeleteHolonBySelectorAsync(string predicate, string value)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var lifetime = session.ConfigureAwait(false);
                var deletedId = await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync($@"
                        MATCH (hl:Holon) WHERE {predicate}
                        RETURN hl.GUId AS id
                        LIMIT 2", new { value }).ConfigureAwait(false);
                    var rows = await cursor.ToListAsync().ConfigureAwait(false);
                    if (rows.Count == 0)
                        return (Guid?)null;
                    if (rows.Count > 1)
                        throw new StorageIdentityAmbiguousException();

                    var id = Guid.Parse(rows[0]["id"].As<string>());
                    var deletion = await transaction.RunAsync($@"
                        MATCH (hl:Holon) WHERE {predicate} AND hl.GUId = $guid
                        DETACH DELETE hl", new { value, guid = id.ToString("D") }).ConfigureAwait(false);
                    var summary = await deletion.ConsumeAsync().ConfigureAwait(false);
                    if (summary.Counters.NodesDeleted != 1)
                        throw new InvalidOperationException("Neo4j Aura did not delete exactly one holon.");
                    return id;
                }).ConfigureAwait(false);

                if (!deletedId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura holon was not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.Result = new Holon { Id = deletedId.Value };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error deleting Neo4j Aura holon.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
                }

                StoredRecordSearch.Validate(searchParams, version, maxChildDepth);
                var groups = searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var wantsAvatars = groups.Count == 0 || groups.Any(group => group.SearchAvatars || !group.SearchHolons);
                var wantsHolons = groups.Count == 0 || groups.Any(group => group.SearchHolons || !group.SearchAvatars);
                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                if (wantsHolons || searchParams.ParentId != Guid.Empty)
                {
                    var loaded = await LoadAllHolonsAsync().ConfigureAwait(false);
                    if (loaded.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, loaded.Message, loaded.Exception);
                        return result;
                    }
                    holons = loaded.Result.ToList();
                }
                if (wantsAvatars)
                {
                    var loaded = await LoadAllAvatarsAsync().ConfigureAwait(false);
                    if (loaded.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, loaded.Message, loaded.Exception);
                        return result;
                    }
                    searchResults.SearchResultAvatars.AddRange(
                        loaded.Result.Where(row => StoredRecordSearch.Matches(row, true, searchParams, holons)));
                }
                if (wantsHolons)
                    searchResults.SearchResultHolons.AddRange(
                        holons.Where(row => StoredRecordSearch.Matches(row, false, searchParams, holons)));

                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                result.Result = searchResults;
                result.IsLoaded = true;
                result.LoadedCount = searchResults.NumberOfResults;
                result.Message = $"Successfully searched Neo4j Aura database and found {searchResults.NumberOfResults} results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching Neo4j Aura database: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
                }

                var batch = await SaveHolonsAsync(holons, continueOnError: false).ConfigureAwait(false);
                result.SavedCount = batch.SavedCount;
                if (batch.IsError || !batch.IsSaved)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"Neo4j Aura import stopped after {batch.SavedCount} acknowledged saves: {batch.Message}", batch.Exception);
                    return result;
                }

                result.Result = true;
                result.IsSaved = true;
                result.Message = $"Successfully imported {batch.SavedCount} holons to Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Neo4j Aura: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons for avatar from Neo4j Aura
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Neo4j Aura: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Export all holons for this avatar
                    var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                    if (holonsResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                        return result;
                    }

                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Neo4j Aura";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

    }
}
