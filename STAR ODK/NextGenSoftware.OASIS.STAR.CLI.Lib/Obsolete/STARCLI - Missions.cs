//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static async Task<OASISResult<IMission>> CreateMissionAsync(object createParams = null, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IMission> result = new OASISResult<IMission>();

//            string name = CLIEngine.GetValidInput("What is the name of the mission?");
//            string description = CLIEngine.GetValidInput("What is the description of the mission?");

//            //result = await STAR.OASISAPI.Missions.CreateMissionAsync(name, description, STAR.BeamedInAvatar.Id, providerType); //TODO: Not sure which way is better?
//            result = await STAR.OASISAPI.Missions.SaveMissionAsync(new Mission()
//            {
//                Name = name,
//                Description = description,
//            }, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Mission Successfully Created.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured creating the mission. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IMission>> PublishMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IMission> result = await STAR.OASISAPI.Missions.PublishMissionAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Mission Successfully Published.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured publishing the mission. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IMission>> UnpublishMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IMission> result = await STAR.OASISAPI.Missions.UnpublishMissionAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Mission Successfully Unpublished.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured unpublishing the mission. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IMission>>> ListAllMissionsForBeamedInAvatar(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IMission>> result = await STAR.OASISAPI.Missions.LoadAllMissionsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType);
//            ListMissions(result);
//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IMission>>> ListAllMissions(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IMission>> result = await STAR.OASISAPI.Missions.LoadAllMissionsAsync(providerType);
//            ListMissions(result);
//            return result;
//        }

//        public static async Task SearchMissionsAsync(string searchTerm, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            ListMissions(await STAR.OASISAPI.Missions.SearchMissionsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar, providerType));
//        }

//        public static async Task<OASISResult<IMission>> ShowMission(Guid missionId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IMission> result = await STAR.OASISAPI.Missions.LoadMissionAsync(missionId, providerType);

//            if (result != null && result.Result != null && !result.IsError)
//                ShowMission(result.Result);

//            return result;
//        }

//        public static void ShowMission(IMission mission)
//        {
//            CLIEngine.ShowMessage(string.Concat($"Id: ", mission.Id != Guid.Empty ? mission.Id : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created On: ", mission.CreatedDate != DateTime.MinValue ? mission.CreatedDate.ToString() : "None"));
//            //CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatarUsername, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatar.Username, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published On: ", mission.PublishedOn != DateTime.MinValue ? mission.PublishedOn.ToString() : "None"));
//            //CLIEngine.ShowMessage(string.Concat($"Published By: ", mission.PublishedByAvatarId != Guid.Empty ? string.Concat(mission.PublishedByAvatarUsername, " (", mission.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published By: ", mission.PublishedByAvatarId != Guid.Empty ? string.Concat(mission.PublishedByAvatar.Username, " (", mission.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Version: ", mission.Version));
//        }

//        private static void ListMissions(OASISResult<IEnumerable<IMission>> missions)
//        {
//            if (missions != null)
//            {
//                if (!missions.IsError)
//                {
//                    if (missions.Result != null && missions.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (missions.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{missions.Result.Count()} Mission Found:");
//                        else
//                            CLIEngine.ShowMessage($"{missions.Result.Count()} Mission's Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IOAPP oapp in missions.Result)
//                            ShowOAPP(oapp);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No Mission's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading Mission's. Reason: {missions.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading Mission's.");
//        }
//    }
//}

