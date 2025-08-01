//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static async Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpotAsync(object createParams = null, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
//            byte[] object3D = null;
//            byte[] image2D = null;
//            List<IInventoryItem> rewards = new List<IInventoryItem>();
//            List<string> rewardIds = new List<string>();
//            string name = CLIEngine.GetValidInput("What is the name of the GeoHotSpot?");
//            string description = CLIEngine.GetValidInput("What is the description of the GeoHotSpot?");
//            GeoHotSpotTriggeredType triggerType = (GeoHotSpotTriggeredType)CLIEngine.GetValidInputForEnum("What will trigger the GeoHotSpot?", typeof(GeoHotSpotTriggeredType));
//            double latLocation = CLIEngine.GetValidInputForDouble("What is the lat geo-location for the GeoHotSpot?");
//            double longLocation = CLIEngine.GetValidInputForDouble("What is the long geo-location for the GeoHotSpot?");
//            int radius = CLIEngine.GetValidInputForInt("What is the radius in metres for the GeoHotSpot? (the player/avatar needs to be within this radius to trigger the hotspot)");
//            int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3;
//            int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3;

//            if (triggerType == GeoHotSpotTriggeredType.WhenAtGeoLocationForXSeconds)
//                timeInSecondsNeedToBeAtLocationToTriggerHotSpot = CLIEngine.GetValidInputForInt("How many seconds does the avatar/player need to be at the geo location to trigger the GeoHotSpot?");

//            if (triggerType == GeoHotSpotTriggeredType.WhenLookingAtObjectOrImageForXSecondsInARMode)
//                timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = CLIEngine.GetValidInputForInt("How many seconds does the avatar/player need to look at the 2D Image/Sprite or 3D Object to trigger the GeoHotSpot?");

//            if (CLIEngine.GetConfirmation("Would you like a 2D Sprite/Image or a 3D Object to appear in the geolocation? (Press 'Y' for 3DObject or 'N' for 2D Sprite/Image)"))
//                object3D = CLIEngine.GetValidFileAndUpload("What is the full path to the 3D Object? If TriggerType is WhenLookingAtObjectOrSpriteInARMode or WhenObjectOrSpriteIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.");
//            else
//                image2D = CLIEngine.GetValidFileAndUpload("What is the full path to the 2D Sprite/Image? If TriggerType is WhenLookingAtObjectOrSpriteInARMode or WhenObjectOrSpriteIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.");

//            if (CLIEngine.GetConfirmation("Would you like a reward to be granted to the player/avatar once the GeoHotSpot has been triggered. (This reward will be placed in the avatar's inventory.)"))
//            {
//                OASISResult<IInventoryItem> inventoryItemResult = null;
//                bool addMoreRewards = true;

//                do
//                {
//                    if (CLIEngine.GetConfirmation("Have you already created the inventory item to be rewarded?"))
//                    {
//                        Guid inventoryItemId = CLIEngine.GetValidInputForGuid("What is the ID/GUID of the inventory item?");
//                        inventoryItemResult = await STAR.OASISAPI.Inventory.LoadInventoryItemAsync(inventoryItemId, STAR.BeamedInAvatar.Id, providerType);
//                    }
//                    else if (CLIEngine.GetConfirmation("Would you like to create the inventory item now?"))
//                        inventoryItemResult = await CreateInventoryItemAsync(providerType);

//                    if (inventoryItemResult != null && !inventoryItemResult.IsError && inventoryItemResult.Result != null)
//                    {
//                        int quantity = CLIEngine.GetValidInputForInt("How many would you like to add?");

//                        for (int i = 0; i < quantity; i++)
//                        {
//                            rewards.Add(inventoryItemResult.Result);
//                            rewardIds.Add(inventoryItemResult.Result.Id.ToString());
//                        }
//                    }

//                    if (!CLIEngine.GetConfirmation("Would you like to add more rewards?"))
//                        addMoreRewards = false;

//                } while (addMoreRewards);
//            }

//            //result = await STAR.OASISAPI.GeoHotSpots.CreateGeoHotSpotAsync(name, description, STAR.BeamedInAvatar.Id, triggerType, threeDObject, twoDObject, providerType);
//            result = await STAR.OASISAPI.GeoHotSpots.SaveGeoHotSpotAsync(new GeoHotSpot()
//            {
//                Name = name,
//                Description = description,
//                Lat = latLocation,
//                Long = longLocation,
//                HotSpotRadiusInMetres = radius,
//                TriggerType = triggerType,
//                TimeInSecondsNeedToBeAtLocationToTriggerHotSpot = timeInSecondsNeedToBeAtLocationToTriggerHotSpot,
//                Object3D = object3D,
//                Image2D = image2D,
//                TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot,
//                Rewards = rewards
//            }, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("GeoHotSpot Successfully Created.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured creating the GeoHotSpot. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IGeoHotSpot>> PublishGeoHotSpotAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IGeoHotSpot> result = await STAR.OASISAPI.GeoHotSpots.PublishGeoHotSpotAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("GeoHotSpot Successfully Published.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured publishing the mission. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IGeoHotSpot>> UnpublishGeoHotSpotAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IGeoHotSpot> result = await STAR.OASISAPI.GeoHotSpots.UnpublishGeoHotSpotAsync(missionId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("GeoHotSpot Successfully Unpublished.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured unpublishing the mission. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IGeoHotSpot>>> ListAllGeoHotSpotsForBeamedInAvatar(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IGeoHotSpot>> result = await STAR.OASISAPI.GeoHotSpots.LoadAllGeoHotSpotsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType);
//            ListGeoHotSpots(result);
//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IGeoHotSpot>>> ListAllGeoHotSpots(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IGeoHotSpot>> result = await STAR.OASISAPI.GeoHotSpots.LoadAllGeoHotSpotsAsync(providerType);
//            ListGeoHotSpots(result);
//            return result;
//        }

//        public static async Task SearchGeoHotSpotsAsync(string searchTerm, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            ListGeoHotSpots(await STAR.OASISAPI.GeoHotSpots.SearchGeoHotSpotsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar,  providerType));
//        }

//        public static async Task<OASISResult<IGeoHotSpot>> ShowGeoHotSpot(Guid missionId, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IGeoHotSpot> result = await STAR.OASISAPI.GeoHotSpots.LoadGeoHotSpotAsync(missionId, providerType);

//            if (result != null && result.Result != null && !result.IsError)
//                ShowGeoHotSpot(result.Result);

//            return result;
//        }

//        public static void ShowGeoHotSpot(IGeoHotSpot mission)
//        {
//            CLIEngine.ShowMessage(string.Concat($"Id: ", mission.Id != Guid.Empty ? mission.Id : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(mission.Name) ? mission.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created On: ", mission.CreatedDate != DateTime.MinValue ? mission.CreatedDate.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created By: ", mission.CreatedByAvatarId != Guid.Empty ? string.Concat(mission.CreatedByAvatar.Username, " (", mission.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published On: ", mission.PublishedOn != DateTime.MinValue ? mission.PublishedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published By: ", mission.PublishedByAvatarId != Guid.Empty ? string.Concat(mission.PublishedByAvatar.Username, " (", mission.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Version: ", mission.Version));
//        }

//        private static void ListGeoHotSpots(OASISResult<IEnumerable<IGeoHotSpot>> geoHotSpots)
//        {
//            if (geoHotSpots != null)
//            {
//                if (!geoHotSpots.IsError)
//                {
//                    if (geoHotSpots.Result != null && geoHotSpots.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (geoHotSpots.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{geoHotSpots.Result.Count()} GeoHotSpot Found:");
//                        else
//                            CLIEngine.ShowMessage($"{geoHotSpots.Result.Count()} GeoHotSpot's Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IOAPP oapp in geoHotSpots.Result)
//                            ShowOAPP(oapp);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No GeoHotSpot's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading GeoHotSpot's. Reason: {geoHotSpots.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading GeoHotSpot's.");
//        }
//    }
//}
