using System;
using System.IO;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Globalization;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class GeoHotSpots : STARNETUIBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, GeoHotSpotDNA>
    public class GeoHotSpots : STARNETUIBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, STARNETDNA>
    {
        public GeoHotSpots(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.GeoHotSpotManager(avatarId, STARDNA),
            "Welcome to the Geo-HotSpot Wizard", new List<string> 
            {
                "This wizard will allow you create a GeoHotSpot which are triggered at the desired location in Our World or any other game.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "The wizard will create an empty folder with a GeoHotSpot.json file in it. You then simply place any files/folders you need for the assets (optional) for the geohotspot into this folder.",
                "Finally you run the sub-command 'geohotspot publish' to convert the folder containing the geohotspot (can contain any number of files and sub-folders) into a OASIS GeoHotSpot file (.ogeohotspot) as well as optionally upload to STARNET.",
                "You can then share the .ogeohotspot file with others across any platform or OS, who can then install the GeoHotSpot from the file using the sub-command 'geohotspot install'.",
                "You can also optionally choose to upload the .ogeohotspot file to the STARNET store so others can search, download and install the geohotspot."
            },
            STAR.STARDNA.DefaultGeoHotSpotsSourcePath, "DefaultGeoHotSpotsSourcePath",
            STAR.STARDNA.DefaultGeoHotSpotsPublishedPath, "DefaultGeoHotSpotsPublishedPath",
            STAR.STARDNA.DefaultGeoHotSpotsDownloadedPath, "DefaultGeoHotSpotsDownloadedPath",
            STAR.STARDNA.DefaultGeoHotSpotsInstalledPath, "DefaultGeoHotSpotsInstalledPath")
        { }

        public override async Task<OASISResult<GeoHotSpot>> CreateAsync(ISTARNETCreateOptions<GeoHotSpot, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<GeoHotSpot> result = new OASISResult<GeoHotSpot>();

            if (createOptions?.CustomCreateParams != null
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.Scripted, out object scriptedFlag)
                && scriptedFlag is bool sb && sb
                && createOptions.CustomCreateParams.ContainsKey(StarCliNonInteractiveCreateKeys.GeoHotSpotLat))
            {
                if (createOptions.STARNETHolon == null)
                    createOptions.STARNETHolon = new GeoHotSpot();

                Dictionary<string, object> p = createOptions.CustomCreateParams;
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotLat, out object latO))
                    createOptions.STARNETHolon.Lat = Convert.ToDouble(latO, CultureInfo.InvariantCulture);
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotLong, out object longO))
                    createOptions.STARNETHolon.Long = Convert.ToDouble(longO, CultureInfo.InvariantCulture);
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotRadiusMetres, out object radO))
                    createOptions.STARNETHolon.HotSpotRadiusInMetres = Convert.ToInt32(radO, CultureInfo.InvariantCulture);
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotTriggeredType, out object trO)
                    && trO != null
                    && Enum.TryParse<GeoHotSpotTriggeredType>(trO.ToString(), ignoreCase: true, out GeoHotSpotTriggeredType trig))
                {
                    createOptions.STARNETHolon.TriggerType = trig;
                    if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotTimeSeconds, out object tsO))
                    {
                        int ts = Convert.ToInt32(tsO, CultureInfo.InvariantCulture);
                        if (trig == GeoHotSpotTriggeredType.WhenAtGeoLocationForXSeconds)
                            createOptions.STARNETHolon.TimeInSecondsNeedToBeAtLocationToTriggerHotSpot = ts;
                        else if (trig == GeoHotSpotTriggeredType.WhenLookingAtObjectOrImageForXSecondsInARMode)
                            createOptions.STARNETHolon.TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = ts;
                    }
                }

                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotAudioUrl, out object audioO) && audioO != null)
                    createOptions.STARNETHolon.AudioUrl = audioO.ToString()?.Trim() ?? string.Empty;
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotVideoUrl, out object videoO) && videoO != null)
                    createOptions.STARNETHolon.VideoUrl = videoO.ToString()?.Trim() ?? string.Empty;

                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotAudioFilePath, out object audioPathO) && audioPathO != null)
                {
                    string ap = audioPathO.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(ap))
                    {
                        if (!File.Exists(ap))
                        {
                            result.IsError = true;
                            result.Message = $"Geo-hotspot audio file not found: {ap}";
                            return result;
                        }
                        try
                        {
                            createOptions.STARNETHolon.AudioData = File.ReadAllBytes(ap);
                            createOptions.STARNETHolon.AudioUrl = null;
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            result.IsError = true;
                            result.Message = $"Could not read geo-hotspot audio file: {ex.Message}";
                            return result;
                        }
                    }
                }

                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotVideoFilePath, out object videoPathO) && videoPathO != null)
                {
                    string vp = videoPathO.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(vp))
                    {
                        if (!File.Exists(vp))
                        {
                            result.IsError = true;
                            result.Message = $"Geo-hotspot video file not found: {vp}";
                            return result;
                        }
                        try
                        {
                            createOptions.STARNETHolon.VideoData = File.ReadAllBytes(vp);
                            createOptions.STARNETHolon.VideoUrl = null;
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            result.IsError = true;
                            result.Message = $"Could not read geo-hotspot video file: {ex.Message}";
                            return result;
                        }
                    }
                }

                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotTextContent, out object textO) && textO != null)
                    createOptions.STARNETHolon.TextContent = textO.ToString()?.Trim() ?? string.Empty;
                if (p.TryGetValue(StarCliNonInteractiveCreateKeys.GeoHotSpotWebsiteUrl, out object webO) && webO != null)
                    createOptions.STARNETHolon.WebsiteUrl = webO.ToString()?.Trim() ?? string.Empty;

                return await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, addDependencies, providerType);
            }

            //if (CLIEngine.GetConfirmation("Does this GeoHotSpot belong to a quest?"))
            //{
            //    OASISResult<InstalledQuest> questResult = await STARCLI.Quests.FindAndInstallIfNotInstalledAsync("use for the parent");

            //    if (questResult != null && questResult.Result != null && !questResult.IsError)
            //    {
            //        OASISResult<Quest> loadResult = await STAR.STARAPI.Quests.LoadAsync(STAR.BeamedInAvatar.Id, questResult.Result.Id, providerType: providerType);

            //        if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            //            parentQuest = loadResult.Result;
            //    }
            //}

            ShowHeader();

            if (createOptions == null)
                createOptions = new STARNETCreateOptions<GeoHotSpot, STARNETDNA>() { STARNETHolon = new GeoHotSpot() };

            //if (parentQuest != null)
            //    newHolon.ParentQuestId = parentQuest.Id;

            object hotspotTypeObj = CLIEngine.GetValidInputForEnum("Select the GeoHotSpot type (Map, AR, VR, IR for map/AR assets; Audio, Video, Text, WebsiteLink for media or links):", typeof(GeoHotSpotType));
            if (hotspotTypeObj == null || hotspotTypeObj.ToString() == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            var hotspotType = (GeoHotSpotType)hotspotTypeObj;
            bool isMediaType = hotspotType == GeoHotSpotType.Audio || hotspotType == GeoHotSpotType.Video
                || hotspotType == GeoHotSpotType.Text || hotspotType == GeoHotSpotType.WebsiteLink;

            createOptions.STARNETHolon.Lat = CLIEngine.GetValidInputForDouble("Enter the latitude co-ordinates for the GeoHotSpot (i.e. the north-south position): ");
            createOptions.STARNETHolon.Long = CLIEngine.GetValidInputForDouble("Enter the longitude co-ordinates for the GeoHotSpot (i.e. the east-west position): ");
            createOptions.STARNETHolon.HotSpotRadiusInMetres = CLIEngine.GetValidInputForInt("Enter the radius in metres for the GeoHotSpot (i.e. how close you need to be to the lat/long co-ords to trigger the hot-spot): ");
            object triggerTypeObj = CLIEngine.GetValidInputForEnum("Select the trigger type for the GeoHotSpot:", typeof(GeoHotSpotTriggeredType));

            if (triggerTypeObj.ToString() != "exit")
                createOptions.STARNETHolon.TriggerType = (GeoHotSpotTriggeredType)triggerTypeObj;

            switch (createOptions.STARNETHolon.TriggerType)
            {
                case GeoHotSpotTriggeredType.WhenAtGeoLocationForXSeconds:
                    createOptions.STARNETHolon.TimeInSecondsNeedToBeAtLocationToTriggerHotSpot = CLIEngine.GetValidInputForInt("Enter the time in seconds you need to be at the location to trigger the GeoHotSpot: ");
                    break;

                case GeoHotSpotTriggeredType.WhenLookingAtObjectOrImageForXSecondsInARMode:
                    createOptions.STARNETHolon.TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = CLIEngine.GetValidInputForInt("Enter the time in seconds you need to look at the 3D object or 2D image to trigger the GeoHotSpot: ");
                    break;
            }

            if (isMediaType)
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage("Media / link hotspots: provide the payload for the selected type (empty skips that field).", ConsoleColor.DarkCyan, false);
                switch (hotspotType)
                {
                    case GeoHotSpotType.Audio:
                        Console.WriteLine("");
                        if (CLIEngine.GetConfirmation("Use a local audio file (Y) or a streaming / HTTPS URL (N)?"))
                        {
                            Console.WriteLine("");
                            string audioPath = CLIEngine.GetValidFile("What is the full path to the local audio file?");
                            if (audioPath == "exit")
                            {
                                result.Message = "User Exited";
                                return result;
                            }
                            if (!string.IsNullOrWhiteSpace(audioPath))
                            {
                                createOptions.STARNETHolon.AudioData = File.ReadAllBytes(audioPath);
                                createOptions.STARNETHolon.AudioUrl = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine("");
                            createOptions.STARNETHolon.AudioUrl = CLIEngine.GetValidInput("What is the URL to the audio (https or stream)? Leave empty to skip.")?.Trim() ?? string.Empty;
                            createOptions.STARNETHolon.AudioData = null;
                        }
                        break;
                    case GeoHotSpotType.Video:
                        Console.WriteLine("");
                        if (CLIEngine.GetConfirmation("Use a local video file (Y) or a streaming / HTTPS URL (N)?"))
                        {
                            Console.WriteLine("");
                            string videoPath = CLIEngine.GetValidFile("What is the full path to the local video file?");
                            if (videoPath == "exit")
                            {
                                result.Message = "User Exited";
                                return result;
                            }
                            if (!string.IsNullOrWhiteSpace(videoPath))
                            {
                                createOptions.STARNETHolon.VideoData = File.ReadAllBytes(videoPath);
                                createOptions.STARNETHolon.VideoUrl = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine("");
                            createOptions.STARNETHolon.VideoUrl = CLIEngine.GetValidInput("What is the URL to the video? Leave empty to skip.")?.Trim() ?? string.Empty;
                            createOptions.STARNETHolon.VideoData = null;
                        }
                        break;
                    case GeoHotSpotType.Text:
                        createOptions.STARNETHolon.TextContent = CLIEngine.GetValidInput("Text to display when triggered:")?.Trim() ?? string.Empty;
                        break;
                    case GeoHotSpotType.WebsiteLink:
                        createOptions.STARNETHolon.WebsiteUrl = CLIEngine.GetValidInput("Website or deep-link URL:")?.Trim() ?? string.Empty;
                        break;
                }
            }
            else
            {
                OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("GeoHotSpot");

                if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
                {
                    createOptions.STARNETHolon.Image2D = imageObjectResult.Result.Image2D;
                    createOptions.STARNETHolon.Image2DURI = imageObjectResult.Result.Image2DURI;
                    createOptions.STARNETHolon.Object3D = imageObjectResult.Result.Object3D;
                    createOptions.STARNETHolon.Object3DURI = imageObjectResult.Result.Object3DURI;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Error processing image or object!";
                    return result;
                }
            }

            result = await base.CreateAsync(createOptions, hotspotType, false, false, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    //if (CLIEngine.GetConfirmation("Do you want to add any Reward's (InventoryItem's) to this GeoHotSpot now (These are rewarded once the GeoHotSpot has been triggered)?"))
                    //{
                    //    do
                    //    {
                    //        Guid inventoryId = Guid.Empty;
                    //        Console.WriteLine("");
                    //        if (!CLIEngine.GetConfirmation("Does the InventoryItem/Reward already exist?"))
                    //        {
                    //            OASISResult<InventoryItem> inventoryResult = await STARCLI.InventoryItems.CreateAsync(null, providerType: providerType);

                    //            if (inventoryResult != null && inventoryResult.Result != null && !inventoryResult.IsError)
                    //                inventoryId = inventoryResult.Result.Id;
                    //        }

                    //        Console.WriteLine("");
                    //        OASISResult<GeoHotSpot> addResult = await AddDependencyAsync(STARNETDNA: result.Result.STARNETDNA, dependencyType: "InventoryItem", idOrNameOfDependency: inventoryId.ToString(), providerType: providerType);
                    //    }
                    //    while (CLIEngine.GetConfirmation("Do you wish to add another InventoryItem/Reward?"));
                    //}

                    //TODO: Not sure which is better? This message or the above commented out code?
                    CLIEngine.ShowMessage("Add any dependencies to the GeoHotSpot below. If for example you want items to be rewarded when it is triggered then add a InventoryItem dependency, if you want it to unlock a new quest then add a Quest dependency and so on. If however this GeoHotSpot belongs to another Quest then you will need to add it as a dependency to that Quest (or use the quest create/edit sub-command).", ConsoleColor.Yellow);
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }

            return result;
        }
    }
}