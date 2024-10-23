using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;
using System.Reflection;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task<OASISResult<IMission>> CreateMissionAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();

            string name = CLIEngine.GetValidInput("What is the name of the mission?");
            string description = CLIEngine.GetValidInput("What is the description of the mission?");

            OASISResult<IMission> missionResult = await STAR.OASISAPI.Missions.CreateMissionAsync(name, description, STAR.BeamedInAvatar.Id, providerType);

            if (missionResult != null && !missionResult.IsError && missionResult.Result != null)
                CLIEngine.ShowSuccessMessage("Mission Successfully Created.");
            else
                CLIEngine.ShowErrorMessage($"Error occured creating the mission. Reason: {missionResult.Message}");

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

        public static async Task<OASISResult<IMission>> ListMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = await STAR.OASISAPI.Missions.LoadMissionAsync(missionId, providerType);

            if (result != null && result.Result != null && !result.IsError)
                ListMission(result.Result);

            return result;
        }

        public static void ListMission(IMission mission)
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

            if (CLIEngine.GetConfirmation("Quests belong to missions, have you already created a mission for this quest?"))
                missionId = CLIEngine.GetValidInputForGuid("What is the mission ID/GUID that the new quest will belong to?");

            else if (CLIEngine.GetConfirmation("Would you like to create a mission now?"))
            {
                OASISResult<IMission> missionResult = await CreateMissionAsync();

                if (missionResult != null && !missionResult.IsError && missionResult.Result != null)
                    missionId = missionResult.Result.Id;
            }

            if (missionId != Guid.Empty)
            {
                string name = CLIEngine.GetValidInput("What is the name of the quest?");
                string description = CLIEngine.GetValidInput("What is the description of the quest?");
                QuestType questType = (QuestType)CLIEngine.GetValidInputForEnum("What type of quest is it?", typeof(QuestType));

                questResult = await STAR.OASISAPI.Quests.CreateQuestAsync(name, description, questType, STAR.BeamedInAvatar.Id, missionId, providerType);

                if (questResult != null && !questResult.IsError && questResult.Result != null)
                    CLIEngine.ShowSuccessMessage("Quest Successfully Created.");
                else
                    CLIEngine.ShowErrorMessage($"Error occured creating the quest. Reason: {questResult.Message}");
            }

            return questResult;
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

        private static void ListHolons(OASISResult<IEnumerable<IHolon>> holons, string holonTypeName, Func<>)
        {
            if (holons != null && holons.Result != null && !holons.IsError)
            {
                if (holons.Result.Count() > 0)
                {
                    Console.WriteLine();

                    if (holons.Result.Count() == 1)
                        CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName} Found:");
                    else
                        CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName}'s Found:");

                    CLIEngine.ShowDivider();

                    foreach (IOAPP oapp in holons.Result)
                        ShowOAPP(oapp);
                }
                else
                    CLIEngine.ShowWarningMessage($"No {holonTypeName}'s Found.");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured loading {holonTypeName}'s. Reason: {holons.Message}");
        }
    }
}

