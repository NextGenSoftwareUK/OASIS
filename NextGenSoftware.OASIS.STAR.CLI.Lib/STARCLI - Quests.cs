using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task<OASISResult<IMission>> CreateMissionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();

            string name = CLIEngine.GetValidInput("What is the name of the mission?");
            string description = CLIEngine.GetValidInput("What is the description of the mission?");

            result = await STAR.OASISAPI.Missions.CreateMissionAsync(name, description, STAR.BeamedInAvatar.Id, providerType);

            if (result != null && !result.IsError && result.Result != null)
                CLIEngine.ShowSuccessMessage("Mission Successfully Created.");
            else
                CLIEngine.ShowErrorMessage($"Error occured creating the mission. Reason: {result.Message}");

            return result;
        }

        public static async Task<OASISResult<IMission>> PublishMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = await STAR.OASISAPI.Missions.PublishMissionAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

            if (result != null && !result.IsError && result.Result != null)
                CLIEngine.ShowSuccessMessage("Mission Successfully Published.");
            else
                CLIEngine.ShowErrorMessage($"Error occured publishing the mission. Reason: {result.Message}");

            return result;
        }

        public static async Task<OASISResult<IMission>> UnpublishMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = await STAR.OASISAPI.Missions.UnpublishMissionAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

            if (result != null && !result.IsError && result.Result != null)
                CLIEngine.ShowSuccessMessage("Mission Successfully Unpublished.");
            else
                CLIEngine.ShowErrorMessage($"Error occured unpublishing the mission. Reason: {result.Message}");

            return result;
        }

        public static async Task<OASISResult<IEnumerable<IMission>>> ListAllMissionsForBeamedInAvatar(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = await STAR.OASISAPI.Missions.LoadAllMissionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType);
            ListMissions(result);
            return result;
        }

        public static async Task<OASISResult<IEnumerable<IMission>>> ListAllMissions(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = await STAR.OASISAPI.Missions.LoadAllMissionsAsync(providerType);
            ListMissions(result);
            return result;
        }

        public static async Task<OASISResult<IMission>> ShowMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = await STAR.OASISAPI.Missions.LoadMissionAsync(missionId, providerType);

            if (result != null && result.Result != null && !result.IsError)
                ShowMission(result.Result);

            return result;
        }

        public static void ShowMission(IMission mission)
        {
            CLIEngine.ShowMessage(string.Concat($"Id: ", mission.Id != Guid.Empty ? mission.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created On: ", mission.CreatedDate != DateTime.MinValue ? mission.CreatedDate.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatarUsername, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatar.Username, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On: ", mission.PublishedOn != DateTime.MinValue ? mission.PublishedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published By: ", mission.PublishedByAvatarId != Guid.Empty ? string.Concat(mission.PublishedByAvatarUsername, " (", mission.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By: ", mission.PublishedByAvatarId != Guid.Empty ? string.Concat(mission.PublishedByAvatar.Username, " (", mission.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Version: ", mission.Version));
        }

        public static async Task<OASISResult<IQuest>> CreateQuestAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> questResult = new OASISResult<IQuest>();
            Guid missionId = Guid.Empty;
            Guid geoNFTId = Guid.Empty;
            Guid geoHotSpotId = Guid.Empty;
            Guid subQuestId = Guid.Empty;

            if (CLIEngine.GetConfirmation("Quests belong to missions, have you already created a mission for this quest/sub-quest?"))
                missionId = CLIEngine.GetValidInputForGuid("What is the mission ID/GUID that the new quest/sub-quest will belong to?");

            else if (CLIEngine.GetConfirmation("Would you like to create a mission now?"))
            {
                OASISResult<IMission> missionResult = await CreateMissionAsync();

                if (missionResult != null && !missionResult.IsError && missionResult.Result != null)
                    missionId = missionResult.Result.Id;
            }

            if (missionId != Guid.Empty)
            {
                string name = CLIEngine.GetValidInput("What is the name of the quest/sub-quest?");
                string description = CLIEngine.GetValidInput("What is the description of the quest/sub-quest?");
                QuestType questType = (QuestType)CLIEngine.GetValidInputForEnum("What type of quest/sub-quest is it?", typeof(QuestType));

                questResult = await STAR.OASISAPI.Quests.CreateQuestAsync(name, description, questType, STAR.BeamedInAvatar.Id, missionId, providerType);

                if (questResult != null && !questResult.IsError && questResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Quest Successfully Created.");

                    if (CLIEngine.GetConfirmation("Do you wish to add any GeoNFT's to this quest?"))
                    {
                        bool addMoreGeoNFTs = true;

                        do
                        {
                            if (CLIEngine.GetConfirmation("Have you already created a OASIS GeoNFT?"))
                                geoNFTId = CLIEngine.GetValidInputForGuid("What is the GeoNFT ID/GUID?");

                            else if (CLIEngine.GetConfirmation("Would you like to create the GeoNFT now?"))
                            {
                                OASISResult<IOASISGeoSpatialNFT> nftResult = await MintGeoNFTAsync();

                                if (nftResult != null && !nftResult.IsError && nftResult.Result != null)
                                    geoNFTId = nftResult.Result.Id;
                            }

                            if (geoNFTId != Guid.Empty)
                            {
                                questResult = await STAR.OASISAPI.Quests.AddGeoNFTToQuestAsync(questResult.Result.Id, geoNFTId, STAR.BeamedInAvatar.Id, providerType);

                                if (questResult != null && !questResult.IsError && questResult.Result != null)
                                {
                                    CLIEngine.ShowSuccessMessage("GeoNFT Successfully Added To Quest.");

                                    if (!CLIEngine.GetConfirmation("Would you like to add another GeoNFT?"))
                                        addMoreGeoNFTs = false;
                                }
                                else
                                    CLIEngine.ShowErrorMessage($"Error occured adding the GeoNFT to the quest. Reason: {questResult.Message}");
                            }
                        } while (addMoreGeoNFTs);
                    }

                    if (CLIEngine.GetConfirmation("Do you wish to add any GeoHotSpot's to this quest?"))
                    {
                        bool addMoreGeoHotSpots = true;

                        do
                        {
                            if (CLIEngine.GetConfirmation("Have you already created a OASIS GeoHotSpot?"))
                                geoHotSpotId = CLIEngine.GetValidInputForGuid("What is the GeoHotSpot ID/GUID?");

                            else if (CLIEngine.GetConfirmation("Would you like to create the GeoHotSpot now?"))
                            {
                                OASISResult<IGeoHotSpot> geoHotSpotResult = await CreateGeoHotSpot();

                                if (geoHotSpotResult != null && !geoHotSpotResult.IsError && geoHotSpotResult.Result != null)
                                    geoHotSpotId = geoHotSpotResult.Result.Id;
                            }

                            if (geoHotSpotId != Guid.Empty)
                            {
                                questResult = await STAR.OASISAPI.Quests.AddGeoHotSpotToQuestAsync(questResult.Result.Id, geoHotSpotId, STAR.BeamedInAvatar.Id, providerType);

                                if (questResult != null && !questResult.IsError && questResult.Result != null)
                                {
                                    CLIEngine.ShowSuccessMessage("GeoHotSpot Successfully Added To Quest.");

                                    if (!CLIEngine.GetConfirmation("Would you like to add another GeoHotSpot?"))
                                        addMoreGeoHotSpots = false;
                                }
                                else
                                    CLIEngine.ShowErrorMessage($"Error occured adding the GeoHotSpot to the quest. Reason: {questResult.Message}");
                            }
                        } while (addMoreGeoHotSpots);
                    }

                    if (CLIEngine.GetConfirmation("Do you wish to add any sub-quest's to this quest?"))
                    {
                        bool addMoreSubQuests = true;

                        do
                        {
                            if (CLIEngine.GetConfirmation("Have you already created the sub-quest?"))
                            {
                                subQuestId = CLIEngine.GetValidInputForGuid("What is the sub-quest ID/GUID?");

                                if (subQuestId != Guid.Empty)
                                    questResult = await STAR.OASISAPI.Quests.AddSubQuestToQuestAsync(questResult.Result.Id, subQuestId, STAR.BeamedInAvatar.Id, providerType);

                            else if (CLIEngine.GetConfirmation("Would you like to create the sub-quest now?"))
                            {
                                OASISResult<IQuest> subQuestResult = await CreateQuestAsync();

                                if (subQuestResult != null && !subQuestResult.IsError && subQuestResult.Result != null)
                                    questResult = await STAR.OASISAPI.Quests.AddSubQuestToQuestAsync(questResult.Result.Id, subQuestResult.Result, STAR.BeamedInAvatar.Id, providerType);
                            }

                            if (questResult != null && !questResult.IsError && questResult.Result != null)
                            {
                                CLIEngine.ShowSuccessMessage("GeoHotSpot Successfully Added To Quest.");

                                if (!CLIEngine.GetConfirmation("Would you like to add another GeoHotSpot?"))
                                    addMoreSubQuests = false;
                            }
                            else
                                CLIEngine.ShowErrorMessage($"Error occured adding the GeoHotSpot to the quest. Reason: {questResult.Message}");
                            }
                        } while (addMoreSubQuests);
                    }

                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured creating the quest. Reason: {questResult.Message}");  
            }

            return questResult;
        }

        public static async Task<OASISResult<IEnumerable<IQuest>>> ListAllQuestsForBeamedInAvatar(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = await STAR.OASISAPI.Quests.LoadAllQuestsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType);
            ListQuests(result);
            return result;
        }

        public static async Task<OASISResult<IEnumerable<IQuest>>> ListAllQuests(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = await STAR.OASISAPI.Quests.LoadAllQuestsAsync(providerType);
            ListQuests(result);
            return result;
        }

        public static async Task<OASISResult<IQuest>> ShowQuest(Guid questId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = await STAR.OASISAPI.Quests.LoadQuestAsync(questId, providerType);

            if (result != null && result.Result != null && !result.IsError)
                ShowQuest(result.Result);

            return result;
        }

        public static void ShowQuest(IQuest quest)
        {
            CLIEngine.ShowMessage(string.Concat($"Id: ", quest.Id != Guid.Empty ? quest.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(quest.Name) ? quest.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(quest.Name) ? quest.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created On: ", quest.CreatedDate != DateTime.MinValue ? quest.CreatedDate.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatarUsername, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By: ", quest.CreatedByAvatarId != Guid.Empty ? string.Concat(quest.CreatedByAvatar.Username, " (", quest.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Version: ", quest.Version));
        }

        private static void ListQuests(OASISResult<IEnumerable<IQuest>> quests)
        {
            if (quests != null && quests.Result != null && !quests.IsError)
            {
                if (quests.Result.Count() > 0)
                {
                    Console.WriteLine();

                    if (quests.Result.Count() == 1)
                        CLIEngine.ShowMessage($"{quests.Result.Count()} Quest Found:");
                    else
                        CLIEngine.ShowMessage($"{quests.Result.Count()} Quest's Found:");

                    CLIEngine.ShowDivider();

                    foreach (IOAPP oapp in quests.Result)
                        ShowOAPP(oapp);
                }
                else
                    CLIEngine.ShowWarningMessage("No Quest's Found.");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured loading Quest's. Reason: {quests.Message}");
        }

        private static void ListMissions(OASISResult<IEnumerable<IMission>> missions)
        {
            if (missions != null && missions.Result != null && !missions.IsError)
            {
                if (missions.Result.Count() > 0)
                {
                    Console.WriteLine();

                    if (missions.Result.Count() == 1)
                        CLIEngine.ShowMessage($"{missions.Result.Count()} Mission Found:");
                    else
                        CLIEngine.ShowMessage($"{missions.Result.Count()} Mission's Found:");

                    CLIEngine.ShowDivider();

                    foreach (IOAPP oapp in missions.Result)
                        ShowOAPP(oapp);
                }
                else
                    CLIEngine.ShowWarningMessage("No Mission's Found.");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured loading Mission's. Reason: {missions.Message}");
        }

        //private static void ListHolons(OASISResult<IEnumerable<IHolon>> holons, string holonTypeName, Func<>)
        //{
        //    if (holons != null && holons.Result != null && !holons.IsError)
        //    {
        //        if (holons.Result.Count() > 0)
        //        {
        //            Console.WriteLine();

        //            if (holons.Result.Count() == 1)
        //                CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName} Found:");
        //            else
        //                CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName}'s Found:");

        //            CLIEngine.ShowDivider();

        //            foreach (IOAPP oapp in holons.Result)
        //                ShowOAPP(oapp);
        //        }
        //        else
        //            CLIEngine.ShowWarningMessage($"No {holonTypeName}'s Found.");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured loading {holonTypeName}'s. Reason: {holons.Message}");
        //}
    }
}

