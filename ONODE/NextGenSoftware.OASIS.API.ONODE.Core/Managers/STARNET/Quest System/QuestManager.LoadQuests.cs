using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{    public partial class QuestManager
    {
        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentByMetaDataAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IQuest>> LoadAllQuestsForMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadHolonsByMetaData<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentByMetaDataAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        /// <summary>Load all quests for avatar using IQuest path; promotes MetaData to strongly-typed properties (e.g. Status from MetaData["QuestStatus"]).</summary>
        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IQuest>>();
            var baseResult = await LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType).ConfigureAwait(false);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(baseResult, result);
            if (baseResult.IsError || baseResult.Result == null)
                return result;
            var list = baseResult.Result.ToList();
            foreach (var q in list)
                PromoteQuestMetaDataToProperties(q);
            result.Result = list;
            return result;
        }

        /// <summary>Load all quests for avatar using IQuest path; promotes MetaData to strongly-typed properties.</summary>
        public OASISResult<IEnumerable<IQuest>> LoadAllQuestsForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IQuest>>();
            var baseResult = LoadAllForAvatar(avatarId, showAllVersions, version, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(baseResult, result);
            if (baseResult.IsError || baseResult.Result == null)
                return result;
            var list = baseResult.Result.ToList();
            foreach (var q in list)
                PromoteQuestMetaDataToProperties(q);
            result.Result = list;
            return result;
        }

        /// <summary>Promote MetaData to strongly-typed Quest properties so API returns quests with Objectives (and Status) populated on first load.
        /// Status: prefer "Status" (HolonManager.MapMetaData), fallback "QuestStatus".
        /// Objectives: deserialize <c>MetaData["Objectives"]</c> JSON when the quest has no objectives yet; if still empty, synthesize from child <see cref="Quest"/> holons.</summary>
        private static void PromoteQuestMetaDataToProperties(Quest q)
        {
            if (q == null) return;

            if (q.MetaData != null)
            {
                var statusKey = q.MetaData.Keys.FirstOrDefault(k => string.Equals(k, "Status", StringComparison.OrdinalIgnoreCase))
                    ?? q.MetaData.Keys.FirstOrDefault(k => string.Equals(k, "QuestStatus", StringComparison.OrdinalIgnoreCase));
                if (statusKey != null && q.MetaData[statusKey] != null)
                {
                    var s = q.MetaData[statusKey].ToString();
                    if (!string.IsNullOrEmpty(s) && System.Enum.TryParse<QuestStatus>(s, true, out var status))
                        q.Status = status;
                }

                var objectivesKey = q.MetaData.Keys.FirstOrDefault(k => string.Equals(k, "Objectives", StringComparison.OrdinalIgnoreCase));
                if (objectivesKey != null && q.MetaData[objectivesKey] != null && (q.Objectives == null || q.Objectives.Count == 0))
                {
                    try
                    {
                        var raw = q.MetaData[objectivesKey];
                        if (raw is string jsonStr)
                        {
                            var list = DeserializeObjectivesFromMetaDataJsonString(jsonStr);
                            if (list != null && list.Count > 0)
                                q.Objectives = list;
                        }
                    }
                    catch { /* leave Objectives unchanged if deserialize fails */ }
                }
            }

            /* When Objectives is still empty but Children is populated (e.g. provider loaded child holons), fill Objectives from Children so the API serializes "objectives" and the client does not rely only on "children". */
            if ((q.Objectives == null || q.Objectives.Count == 0) && q.Children != null && q.Children.Count > 0)
            {
                q.Objectives ??= new List<Objective>();
                for (var i = 0; i < q.Children.Count; i++)
                {
                    if (q.Children[i] is Quest cq)
                    {
                        q.Objectives.Add(new Objective
                        {
                            Id = cq.Id,
                            Order = i,
                            IsCompleted = cq.CompletedOn != default,
                            Title = cq.Name ?? string.Empty,
                            Description = cq.Description ?? string.Empty
                        });
                    }
                }
            }

            if (q.Objectives != null)
            {
                foreach (var o in q.Objectives)
                    o?.EnsureAuthoredStringsFromComputedProgress();
            }
        }

        private static readonly JsonSerializerOptions ObjectiveMetaDataJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Deserialize persisted <c>Objectives</c> array from MetaData; JSON matches <see cref="Objective"/> (Title, Description, ProgressSummary, dictionaries).</summary>
        private static List<Objective>? DeserializeObjectivesFromMetaDataJsonString(string jsonStr)
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<Objective>>(jsonStr, ObjectiveMetaDataJsonOptions);
                return list != null && list.Count > 0 ? list : null;
            }
            catch
            {
                return null;
            }
        }

        //public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<IOASISGeoSpatialNFT> nftResult = await NFTManager.LoadGeoNftAsync(geoNFTId, providerType);

        //            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());

        //                //if (parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] != null)
        //                //{
        //                //    Dictionary<string, List<STARNETDependency> geoNFTs = parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] as Dictionary<string, string>;

        //                //    if (geoNFTs != null)
        //                //    {
        //                //        geoNFTs[]
        //                //    }
        //                //}

        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNftAsync. Reason: {nftResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> AddGeoNFTToQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<IOASISGeoSpatialNFT> nftResult = NFTManager.LoadGeoNft(geoNFTId, providerType);

        //            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNft. Reason: {nftResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

        //            if (geoNFT != null)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

        //            if (geoNFT != null)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<GeoHotSpot> geoHotSpotResult = await Data.LoadHolonAsync<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
        //                parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolonAsync. Reason: {geoHotSpotResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> AddGeoHotSpotToQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<GeoHotSpot> geoHotSpotResult = Data.LoadHolon<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
        //                parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {geoHotSpotResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

        //            if (geoHotSpot != null)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
        //                parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

        //            if (geoHotSpot != null)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
        //                parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //TODO: Need to show this on STAR CLI ASAP! ;-)
    }
}