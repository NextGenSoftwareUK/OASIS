using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Drawing.Text;
using System.Linq;
using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using Org.BouncyCastle.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class STARNETUIBase<T1, T2, T3, T4>
    {
        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Deactivated {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListDeactivatedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to reactivate any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to reactivate?");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T1 template = result.Result.ElementAt(number - 1);
                        Guid id = Guid.Empty;

                        if (template != null)
                        {
                            OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                            {
                                await ShowAsync(activateResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Reactivated");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured reactivating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
                        }
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task SearchAsync(string searchTerm = "", Guid parentId = default, bool showAllVersions = false, bool showForAllAvatars = true, ProviderType providerType = ProviderType.Default, int maxResults = 0)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "forallavatars" || searchTerm == "forallavatars")
            {
                //Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName} you wish to search for?");
            }

            // 0 = unlimited (all rows from provider). Per-command maxResults wins; else CLIEngine.MaxHolonSearchResults if > 0.
            int cap = maxResults > 0 ? maxResults : (CLIEngine.MaxHolonSearchResults > 0 ? CLIEngine.MaxHolonSearchResults : 0);

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
            OASISResult<IEnumerable<T1>> r = await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, searchTerm, parentId, null, MetaKeyValuePairMatchMode.All, !showForAllAvatars, showAllVersions, 0, providerType);
            if (cap > 0 && r != null && r.Result != null)
                r.Result = r.Result.Take(cap).ToList();
            ListStarHolons(r);
        }

        public virtual async Task ShowAsync(string idOrName = "", bool showDetailed = false, ProviderType providerType = ProviderType.Default)
        {
            if (idOrName.ToLower() == "detailed")
            {
                idOrName = "";
                showDetailed = true;
            }

            OASISResult<T1> result = await FindAsync("view", idOrName, default, true, providerType: providerType);

            //if (result != null && !result.IsError && result.Result != null)
            //    Show(result.Result, showDetailedInfo: showDetailed);
            //else
            //    CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task ShowAsync<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null) where T : ISTARNETHolon
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                CLIEngine.ShowMessage(string.Concat("Number:".PadRight(displayFieldLength), number), false);

            CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), starHolon.STARNETDNA.Id != Guid.Empty ? starHolon.STARNETDNA.Id : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Name:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Name) ? starHolon.STARNETDNA.Name : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Description:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.Description) ? starHolon.STARNETDNA.Description : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Type:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETHolonType), false);
            CLIEngine.ShowMessage(string.Concat($"Category:".PadRight(displayFieldLength), FormatStarnetDnaCategoryForDisplay(starHolon.STARNETDNA.STARNETCategory)), false);
            
            // Display Language (STARNETSubCategory) for libraries
            if (starHolon.STARNETDNA.STARNETSubCategory != null)
            {
                CLIEngine.ShowMessage(string.Concat($"Language:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETSubCategory), false);
            }
            CLIEngine.ShowMessage(string.Concat($"Created On:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Created By:".PadRight(displayFieldLength), starHolon.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.CreatedByAvatarUsername, " (", starHolon.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified On:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedOn != DateTime.MinValue ? starHolon.STARNETDNA.CreatedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Modified By:".PadRight(displayFieldLength), starHolon.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.ModifiedByAvatarUsername, " (", starHolon.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Source Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.SourcePath) ? starHolon.STARNETDNA.SourcePath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published On:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOn != DateTime.MinValue ? starHolon.STARNETDNA.PublishedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published By:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(starHolon.STARNETDNA.PublishedByAvatarUsername, " (", starHolon.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Published Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.PublishedPath) ? starHolon.STARNETDNA.PublishedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Filesize:".PadRight(displayFieldLength), starHolon.STARNETDNA.FileSize.ToString()), false);
            CLIEngine.ShowMessage(string.Concat($"Published On STARNET:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedOnSTARNET ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To Cloud:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedToCloud ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"Published To OASIS Provider:".PadRight(displayFieldLength), starHolon.STARNETDNA.PublishedProviderType), false);
            CLIEngine.ShowMessage(string.Concat($"Launch Target:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.STARNETDNA.LaunchTarget) ? starHolon.STARNETDNA.LaunchTarget : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.Version), false);
            CLIEngine.ShowMessage(string.Concat($"Version Sequence:".PadRight(displayFieldLength), starHolon.STARNETDNA.VersionSequence), false);
            CLIEngine.ShowMessage(string.Concat($"Number Of Versions:".PadRight(displayFieldLength), starHolon.STARNETDNA.NumberOfVersions), false);
            CLIEngine.ShowMessage(string.Concat($"Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.Downloads), false);
            CLIEngine.ShowMessage(string.Concat($"Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.Installs), false);
            CLIEngine.ShowMessage(string.Concat($"Total Downloads:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalDownloads), false);
            CLIEngine.ShowMessage(string.Concat($"Total Installs:".PadRight(displayFieldLength), starHolon.STARNETDNA.TotalInstalls), false);
            CLIEngine.ShowMessage(string.Concat($"Active:".PadRight(displayFieldLength), starHolon.MetaData != null && starHolon.MetaData.ContainsKey("Active") && starHolon.MetaData["Active"] != null && starHolon.MetaData["Active"].ToString() == "1" ? "True" : "False"), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"OASIS API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.OASISAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.COSMICVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR Runtime Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARRuntimeVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARODKVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STARNET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARNETVersion), false);
            CLIEngine.ShowMessage(string.Concat($"STAR API Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.STARAPIVersion), false);
            CLIEngine.ShowMessage(string.Concat($".NET Version:".PadRight(displayFieldLength), starHolon.STARNETDNA.DotNetVersion), false);
            Console.WriteLine("");

            if (starHolon.STARNETDNA.MetaTagMappings != null)
                ShowHolonMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings.MetaHolonTags, showDetailedInfo, displayFieldLength);
            else
                DisplayProperty("Holon Meta Tag Mappings", "None", displayFieldLength);

            if (starHolon.STARNETDNA.MetaTagMappings != null)
                ShowMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings.MetaTags, showDetailedInfo, displayFieldLength);
            else
                DisplayProperty("Meta Tag Mappings", "None", displayFieldLength);

            ShowAllDependencies(starHolon, showDetailedInfo, displayFieldLength);
            //Console.WriteLine("");
            //ShowHolonMetaTagMappings(starHolon.STARNETDNA.MetaHolonTagMappings, showDetailedInfo, displayFieldLength);
            //ShowMetaTagMappings(starHolon.STARNETDNA.MetaTagMappings, showDetailedInfo, displayFieldLength);

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public virtual void ShowInstalled(T3 starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showUninstallInfo = false, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            //Show((T1)starHolon, showHeader, false, showNumbers, number, showDetailedInfo);
            ShowAsync(ConvertFromT3ToT1(starHolon), showHeader, false, showNumbers, number, showDetailedInfo);

            Console.WriteLine("");
            CLIEngine.ShowMessage(string.Concat($"Downloaded On:".PadRight(displayFieldLength), starHolon.DownloadedOn != DateTime.MinValue ? starHolon.DownloadedOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded By:".PadRight(displayFieldLength), starHolon.DownloadedBy != Guid.Empty ? string.Concat(starHolon.DownloadedByAvatarUsername, " (", starHolon.DownloadedBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Downloaded Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.DownloadedPath) ? starHolon.DownloadedPath : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed On:".PadRight(displayFieldLength), starHolon.InstalledOn != DateTime.MinValue ? starHolon.InstalledOn.ToString() : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed By:".PadRight(displayFieldLength), starHolon.InstalledBy != Guid.Empty ? string.Concat(starHolon.InstalledByAvatarUsername, " (", starHolon.InstalledBy.ToString(), ")") : "None"), false);
            CLIEngine.ShowMessage(string.Concat($"Installed Path:".PadRight(displayFieldLength), !string.IsNullOrEmpty(starHolon.InstalledPath) ? starHolon.InstalledPath : "None"), false);

            if (showUninstallInfo)
            {
                CLIEngine.ShowMessage(string.Concat($"Uninstalled On:".PadRight(displayFieldLength), starHolon.UninstalledOn != DateTime.MinValue ? starHolon.UninstalledOn.ToString() : "None"), false);
                CLIEngine.ShowMessage(string.Concat($"Uninstalled By:".PadRight(displayFieldLength), starHolon.UninstalledBy != Guid.Empty ? string.Concat(starHolon.UninstalledByAvatarUsername, " (", starHolon.UninstalledBy.ToString(), ")") : "None"), false);
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

        public void ShowHeader()
        {
            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage(CreateHeader);
            CLIEngine.ShowDivider();
            Console.WriteLine();

            for (int i = 0; i < CreateIntroParagraphs.Count; i++)
                CLIEngine.ShowMessage(CreateIntroParagraphs[i]);

            CLIEngine.ShowDivider();
        }

        protected void ShowDependency(ISTARNETDependency metaData, int displayFieldLength)
        {
            Console.WriteLine("");
            DisplayProperty("Id", metaData.STARNETHolonId.ToString(), displayFieldLength);
            DisplayProperty("Name", metaData.Name, displayFieldLength);
            DisplayProperty("Description", metaData.Description, displayFieldLength);
            DisplayProperty("Version", metaData.Version, displayFieldLength);
            DisplayProperty("Version Sequence", metaData.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Installed From", metaData.InstalledFrom, displayFieldLength);
            DisplayProperty("Installed To", metaData.InstalledTo, displayFieldLength);
            //Console.WriteLine("");
        }

        protected void ShowDependenices(IList<STARNETDependency> dependencies, int displayFieldLength)
        {
            if (dependencies.Count > 0)
            {
                foreach (ISTARNETDependency dependency in dependencies)
                    ShowDependency(dependency, displayFieldLength);

                Console.WriteLine("");
            }
            //else
            //    CLIEngine.ShowMessage("None", false);
        }

        protected void ShowAllDependencies(ISTARNETHolon starHolon, bool showDetailed, int displayFieldLength)
        {
            string tip = "";

            //if (!showDetailed)
            //    tip = "(use show/list detailed to view)";

            Console.WriteLine("");
            DisplayProperty("DEPENDENCIES (SMART BRICKS)", "", displayFieldLength, false);
           // Console.WriteLine("");
            DisplayDependencyType("OAPPs", starHolon.STARNETDNA.Dependencies.OAPPs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Runtimes", starHolon.STARNETDNA.Dependencies.Runtimes, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Libs", starHolon.STARNETDNA.Dependencies.Libraries, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Templates", starHolon.STARNETDNA.Dependencies.Templates, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Holons", starHolon.STARNETDNA.Dependencies.Holons, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Zomes", starHolon.STARNETDNA.Dependencies.Zomes, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialSpaces", starHolon.STARNETDNA.Dependencies.CelestialSpaces, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialBodies", starHolon.STARNETDNA.Dependencies.CelestialBodies, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("CelestialBodiesMetaDataDNA", starHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("ZomesMetaDataDNA", starHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("HolonsMetaDataDNA", starHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("NFTs", starHolon.STARNETDNA.Dependencies.NFTs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("NFTCollections", starHolon.STARNETDNA.Dependencies.NFTCollections, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoNFTs", starHolon.STARNETDNA.Dependencies.GeoNFTs, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoNFTCollections", starHolon.STARNETDNA.Dependencies.GeoNFTCollections, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("GeoHotSpots", starHolon.STARNETDNA.Dependencies.GeoHotSpots, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Chapters", starHolon.STARNETDNA.Dependencies.Chapters, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Missions", starHolon.STARNETDNA.Dependencies.Missions, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("Quests", starHolon.STARNETDNA.Dependencies.Quests, tip, showDetailed, displayFieldLength);
            DisplayDependencyType("InventoryItems", starHolon.STARNETDNA.Dependencies.InventoryItems, tip, showDetailed, displayFieldLength);

            //DisplayDependencyType("OAPPS", starHolon.STARNETDNA.Dependencies.OAPPs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("RUNTIMES", starHolon.STARNETDNA.Dependencies.Runtimes, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("LIBS", starHolon.STARNETDNA.Dependencies.Libraries, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("TEMPLATES", starHolon.STARNETDNA.Dependencies.Templates, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("HOLONS", starHolon.STARNETDNA.Dependencies.Holons, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("ZOMES", starHolon.STARNETDNA.Dependencies.Zomes, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALSPACES", starHolon.STARNETDNA.Dependencies.CelestialSpaces, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALBODIES", starHolon.STARNETDNA.Dependencies.CelestialBodies, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CELESTIALBODYMETADATA", starHolon.STARNETDNA.Dependencies.CelestialBodiesMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("ZOMESMETADATA", starHolon.STARNETDNA.Dependencies.ZomesMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("HOLONSMETADATA", starHolon.STARNETDNA.Dependencies.HolonsMetaDataDNA, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("NFTS", starHolon.STARNETDNA.Dependencies.NFTs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("NFTCOLLECTIONS", starHolon.STARNETDNA.Dependencies.NFTCollections, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEONFTS", starHolon.STARNETDNA.Dependencies.GeoNFTs, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEONFTCOLLECTIONS", starHolon.STARNETDNA.Dependencies.GeoNFTCollections, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("GEOHOTSPOTS", starHolon.STARNETDNA.Dependencies.GeoHotSpots, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("CHAPTERS", starHolon.STARNETDNA.Dependencies.Chapters, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("MISSIONS", starHolon.STARNETDNA.Dependencies.Missions, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("QUESTS", starHolon.STARNETDNA.Dependencies.Quests, tip, showDetailed, displayFieldLength);
            //DisplayDependencyType("INVENTORYITEMS", starHolon.STARNETDNA.Dependencies.InventoryItems, tip, showDetailed, displayFieldLength);

            if (!showDetailed)
            {
                Console.WriteLine("");
                DisplayProperty("Use 'show/list detailed' command to view dependency details.", "", displayFieldLength, false);
            }
        }

        private void DisplayDependencyType(string dependencyType, List<STARNETDependency> dependencies, string tip, bool showDetailed, int displayFieldLength)
        {
            if (showDetailed)
                Console.WriteLine("");
            
            DisplayProperty(string.Concat(dependencyType, " (", dependencies.Count, ")"), "", displayFieldLength, false);

            if (showDetailed)
                ShowDependenices(dependencies, displayFieldLength);

            //CLIEngine.ShowMessage(string.Concat($"{dependencies.Count} Found.", dependencies.Count > 0 ? tip : ""), false);
        }

        //protected void ShowHolonMetaTagMappings(Dictionary<string, (string, string)> metaHolonTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        protected void ShowHolonMetaTagMappings(List<MetaHolonTag> metaHolonTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (showDetailedInfo)
            {
                if (metaHolonTagMappings != null && metaHolonTagMappings.Count > 0)
                {
                    int colWidth = 20;
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("Holon Meta Tag Mappings", " (", metaHolonTagMappings.Count.ToString(), "):"), false);
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("TAG".PadRight(20), "HOLON".PadRight(colWidth), "NODE".PadRight(colWidth), "TYPE".PadRight(colWidth)), false);
                    //CLIEngine.ShowMessage(string.Concat("TAG".PadRight(22), "HOLON".PadRight(22), "HOLON TYPE".PadRight(22), "NODE".PadRight(22), "NODE TYPE".PadRight(22)), false);
                    Console.WriteLine("");

                    foreach (MetaHolonTag metaHolonTag in metaHolonTagMappings)
                        CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(colWidth), metaHolonTag.HolonName.PadRight(colWidth), metaHolonTag.NodeName.PadRight(colWidth), metaHolonTag.NodeType.PadRight(colWidth)), false);
                    //CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(22), metaHolonTag.HolonName.PadRight(22), metaHolonTag.NodeName.PadRight(22), metaHolonTag.NodeType.Name.PadRight(22)), false);
                    //CLIEngine.ShowMessage(string.Concat(metaHolonTag.MetaTag.PadRight(22), metaHolonTag.HolonName.PadRight(22), metaHolonTag.NodeName, Enum.GetName(typeof(NodeType), metaHolonTag.NodeType).PadRight(22)), false);

                    Console.WriteLine("");
                }
                else
                    DisplayProperty("Holon Meta Tag Mappings", "None", displayFieldLength);
            }
            else
                DisplayProperty("Holon Meta Tag Mappings", string.Concat(metaHolonTagMappings != null && metaHolonTagMappings.Count > 0 ? metaHolonTagMappings.Count.ToString() : "None", metaHolonTagMappings != null && metaHolonTagMappings.Count > 0 ? " (use show/list detailed to view)" : ""), displayFieldLength);
        }

        protected void ShowMetaTagMappings(Dictionary<string, string> metaTagMappings, bool showDetailedInfo, int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            if (showDetailedInfo)
            {
                if (metaTagMappings != null && metaTagMappings.Count > 0)
                {
                    int colWidth = 20;
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("Meta Tag Mappings", " (", metaTagMappings.Count.ToString(), "):"), false);
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("TAG".PadRight(colWidth), "META DATA".PadRight(colWidth)), false);
                    Console.WriteLine("");

                    foreach (string key in metaTagMappings.Keys)
                        CLIEngine.ShowMessage(string.Concat(key.PadRight(colWidth), metaTagMappings[key].PadRight(colWidth)), false);

                    Console.WriteLine("");
                }
                else
                    DisplayProperty("Meta Tag Mappings", "None", displayFieldLength);
            }
            else
                DisplayProperty("Meta Tag Mappings", string.Concat(metaTagMappings != null && metaTagMappings.Count > 0 ? metaTagMappings.Count.ToString() : "None", metaTagMappings != null && metaTagMappings.Count > 0 ? " (use show/list detailed to view)" : ""), displayFieldLength);
        }

        /// <summary>Non-interactive find by name matched multiple holons; list GUID — name rows (all by default; cap with CLIEngine.MaxHolonSearchResults / --search-limit).</summary>
        private string BuildNonInteractiveMultipleMatchDetails(IEnumerable<T1> matches, string idOrName, string holonUiName)
        {
            if (matches == null)
                return $"Multiple matches for '{idOrName}' ({holonUiName}). Use a GUID in non-interactive mode.";

            IList<T1> list = matches as IList<T1> ?? matches.ToList();
            int total = list.Count;
            int maxList = CLIEngine.MaxHolonSearchResults > 0 ? CLIEngine.MaxHolonSearchResults : total;
            var rows = new List<string>(Math.Min(maxList, total));
            for (int i = 0; i < list.Count && i < maxList; i++)
            {
                T1 h = list[i];
                ISTARNETDNA dna = h?.STARNETDNA;
                string nm = string.IsNullOrWhiteSpace(dna?.Name) ? "(unnamed)" : dna.Name;
                Guid gid = dna != null ? dna.Id : Guid.Empty;
                rows.Add($"  {gid} — {nm}");
            }

            string more = total > maxList ? $"{Environment.NewLine}  ... and {total - maxList} more." : "";
            return $"Multiple matches ({total}) for '{idOrName}' ({holonUiName}). Use a GUID in non-interactive mode.{Environment.NewLine}Candidates:{Environment.NewLine}{string.Join(Environment.NewLine, rows)}{more}";
        }

        public async Task<OASISResult<T1>> FindAsync(string operationName, string idOrName = "", Guid parentId = default, bool showOnlyForCurrentAvatar = false, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;

            if (STARNETHolonUIName == "Default")
                STARNETHolonUIName = STARNETManager.STARNETHolonUIName;

            if (idOrName == Guid.Empty.ToString())
                idOrName = "";

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a GUID or name for this operation. Example: '<command> {operationName} <idOrName>' (entity: {STARNETHolonUIName}).");

                    bool cont = true;
                    OASISResult<IEnumerable<T1>> starHolonsResult = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}'s...");

                        //TODO: Add parentId to load functions below... 
                        if (showOnlyForCurrentAvatar)
                            starHolonsResult = await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId);
                        else
                            starHolonsResult = await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType);

                        ListStarHolons(starHolonsResult);

                        if (!(starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0))
                            cont = false;
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETHolonUIName} you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName}...");
                    result = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.AvatarId, id, 0, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> searchResults = await STARNETManager.SearchAsync<T1>(STAR.BeamedInAvatar.Id, idOrName, parentId, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    BuildNonInteractiveMultipleMatchDetails(searchResults.Result, idOrName, STARNETHolonUIName));

                            ListStarHolons(searchResults, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETHolonUIName} you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                                } while (result.Result == null || result.IsError);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    await ShowAsync(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        if (!CLIEngine.NonInteractive)
                        {
                            //if (((operationName == "view" || operationName == "use") && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                            //    (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                            if (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version}) or do you wish to view all the versions? Press 'Y' for latest version or 'N' for all versions."))
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowWorkingMessage($"Loading {STARNETHolonUIName} Versions...");
                                OASISResult<IEnumerable<T1>> versionsResult = await STARNETManager.LoadVersionsAsync(result.Result.STARNETDNA.Id, providerType);
                                ListStarHolons(versionsResult);

                                if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                                {
                                    bool versionSelected = false;

                                    do
                                    {
                                        int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                        if (version > 0 && version <= versionsResult.Result.Count())
                                        {
                                            versionSelected = true;
                                            result.Result = versionsResult.Result.ElementAt(version - 1);
                                        }
                                        else
                                            CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                        if (version == 0)
                                            break;

                                    } while (!versionSelected);
                                }
                            }
                            else
                                Console.WriteLine("");
                        }

                        if (operationName != "view")
                            await ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.NonInteractive || CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = await CheckIfAlreadyInstalledAsync(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("");
                        result.Result = default;
                        idOrName = "";

                        if (!CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETHolonUIName}?"))
                        {
                            idOrName = "exit";
                            break;
                        }
                    }

                    Console.WriteLine("");
                }

                idOrName = "";
            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }
            else if (idOrName == "nonefound")
            {
                result.IsError = true;
                result.Message = "None Found";
            }

            return result;
        }

        public OASISResult<T1> Find(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, string STARNETHolonUIName = "Default", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            Guid id = Guid.Empty;
            bool reInstall = false;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    if (CLIEngine.NonInteractive)
                        throw new CLIEngineNonInteractiveInputRequiredException(
                            $"Non-interactive mode requires a GUID or name for this operation. Example: '<command> {operationName} <idOrName>' (entity: {STARNETManager.STARNETHolonUIName}).");

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");

                        if (showOnlyForCurrentAvatar)
                            ListStarHolons(STARNETManager.LoadAllForAvatar(STAR.BeamedInAvatar.AvatarId));
                        else
                            ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.AvatarId, null, true, false, 0, providerType: providerType));
                    }
                    else
                        Console.WriteLine("");

                    idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

                    if (idOrName == "exit")
                        break;
                }

                if (addSpace)
                    Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}...");
                    result = STARNETManager.Load(STAR.BeamedInAvatar.Id, id, 0, providerType: providerType);

                    if (result != null && result.Result != null && !result.IsError && showOnlyForCurrentAvatar && result.Result.STARNETDNA.CreatedByAvatarId != STAR.BeamedInAvatar.AvatarId)
                    {
                        CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this {STARNETManager.STARNETHolonUIName}. It was created by another avatar.");
                        result.Result = default;
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage($"Searching {STARNETManager.STARNETHolonUIName}'s...");
                    OASISResult<IEnumerable<T1>> searchResults = STARNETManager.Search(STAR.BeamedInAvatar.Id, idOrName, default, null, MetaKeyValuePairMatchMode.All, showOnlyForCurrentAvatar, false, 0, providerType);

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            if (CLIEngine.NonInteractive)
                                throw new CLIEngineNonInteractiveInputRequiredException(
                                    BuildNonInteractiveMultipleMatchDetails(searchResults.Result, idOrName, STARNETManager.STARNETHolonUIName));

                            ListStarHolons(searchResults, true);

                            do
                            {
                                int number = CLIEngine.GetValidInputForInt($"What is the number of the {STARNETManager.STARNETHolonUIName} you wish to {operationName}?");

                                if (number > 0 && number <= searchResults.Result.Count())
                                    result.Result = searchResults.Result.ElementAt(number - 1);
                                else
                                    CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");

                            } while (result.Result == null || result.IsError);
                        }
                        else if (searchResults.Result.Count() == 1)
                            result.Result = searchResults.Result.FirstOrDefault();
                        else
                        {
                            idOrName = "";
                            CLIEngine.ShowWarningMessage($"No {STARNETManager.STARNETHolonUIName} Found!");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured calling STARNETManager.SearchsAsync. Reason: {searchResults.Message}");
                }

                if (result.Result != null && result.Result.STARNETDNA != null)
                {
                    ShowAsync(result.Result);

                    if (result.Result.STARNETDNA.NumberOfVersions > 1)
                    {
                        if (!CLIEngine.NonInteractive)
                        {
                            if ((operationName == "view" && CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to view the other versions?")) ||
                                (!CLIEngine.GetConfirmation($"{result.Result.STARNETDNA.NumberOfVersions} versions were found. Do you wish to {operationName} the latest version ({result.Result.STARNETDNA.Version})?")))
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName} Versions...");
                                OASISResult<IEnumerable<T1>> versionsResult = STARNETManager.LoadVersions(result.Result.STARNETDNA.Id, providerType);
                                ListStarHolons(versionsResult);

                                if (operationName != "view" && versionsResult != null && versionsResult.Result != null && !versionsResult.IsError && versionsResult.Result.Count() > 0)
                                {
                                    bool versionSelected = false;

                                    do
                                    {
                                        int version = CLIEngine.GetValidInputForInt($"Which version do you wish to {operationName}? (Enter the Version Sequence that corresponds to the relevant template)");

                                        if (version > 0 && version <= versionsResult.Result.Count())
                                        {
                                            versionSelected = true;
                                            result.Result = versionsResult.Result.ElementAt(version - 1);
                                        }
                                        else
                                            CLIEngine.ShowErrorMessage("Invalid version entered. Please try again.");

                                        if (version == 0)
                                            break;

                                    } while (!versionSelected);
                                }
                            }
                            else
                                Console.WriteLine("");
                        }

                        if (operationName != "view")
                            ShowAsync(result.Result);
                    }
                }

                if (idOrName == "exit")
                    break;

                if (result.Result != null && operationName != "view")
                {
                    if (CLIEngine.NonInteractive || CLIEngine.GetConfirmation($"Please confirm you wish to {operationName} this {STARNETManager.STARNETHolonUIName}?"))
                    {
                        if (operationName == "install")
                        {
                            if (result != null && result.Result != null)
                            {
                                OASISResult<T1> checkResult = CheckIfAlreadyInstalled(result.Result, providerType);

                                if (checkResult != null && checkResult.Result != null && !checkResult.IsError)
                                {
                                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall"))
                                        result.MetaData["Reinstall"] = checkResult.MetaData["Reinstall"];
                                }
                                else if (checkResult.IsError)
                                    result.Result = default;
                            }
                            else
                            {
                                CLIEngine.ShowErrorMessage($"Error occured checking if the {STARNETManager.STARNETHolonUIName} is already installed! Reason: Id was not found in the metadata!");
                                result.Result = default;
                            }
                        }

                    }
                    else
                    {
                        if (CLIEngine.GetConfirmation($"Do you wish to search for another {STARNETManager.STARNETHolonUIName}?"))
                            result.Result = default;
                        else
                            break;
                    }

                    Console.WriteLine("");
                }

            }
            while (result.Result == null || result.IsError);

            if (idOrName == "exit")
            {
                result.IsError = true;
                result.Message = "User Exited";
            }

            return result;
        }

        private async Task<OASISResult<T1>> FindForProviderAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProviderAsync, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: providerType);
                }
            }
            else
                result = await FindAsync(operationName, idOrName, default, showOnlyForCurrentAvatar, addSpace, providerType: providerType);

            return result;
        }

        private OASISResult<T1> FindForProvider(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            ProviderType largeFileProviderType = ProviderType.IPFSOASIS;

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation("Do you wish to download from the cloud or from the OASIS? Press 'Y' for the cloud or N' for the OASIS."))
                {
                    Console.WriteLine("");
                    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum($"What OASIS provider do you wish to install the {STARNETManager.STARNETHolonUIName} from? (The default is IPFSOASIS)", typeof(ProviderType));

                    if (largeProviderTypeObject != null)
                    {
                        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Error occured in FindForProvider, reason: largeProviderTypeObject is null!");
                }
                else
                {
                    Console.WriteLine("");
                    result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);
                }
            }
            else
                result = Find(operationName, idOrName, showOnlyForCurrentAvatar, addSpace, providerType: largeFileProviderType);

            return result;
        }


        //public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<T3>> FindForProviderAndInstallIfNotInstalledAsync(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> result = new OASISResult<T3>();
            OASISResult<T1> findResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar, providerType: providerType);

            if (findResult != null && findResult.Result != null && !findResult.IsError)
            {
                //OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);
                OASISResult<bool> installedResult = await STARNETManager.IsInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.Version, providerType);

                if (installedResult != null && !installedResult.IsError)
                {
                    if (!installedResult.Result)
                    {
                        if (CLIEngine.GetConfirmation($"The selected {STARNETManager.STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
                        {
                            result = await DownloadAndInstallAsync(findResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                            if (!(result != null && result.Result != null && !result.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                        }
                        else
                        {
                            Console.WriteLine("");
                            result.Message = "User Declined Installation";
                            result.IsError = true;
                        }
                    }
                    else
                    {
                        result = await STARNETManager.LoadInstalledAsync(STAR.BeamedInAvatar.Id, findResult.Result.STARNETDNA.Id, findResult.Result.STARNETDNA.VersionSequence, providerType);

                        if (!(result != null && result.Result != null && !result.IsError))
                            OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETManager.STARNETHolonUIName} is installed. Reason: {installedResult.Message}");
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage($"Error occured finding {STARNETManager.STARNETHolonUIName}. Reason: {findResult.Message}");
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(findResult, result);
            }

            return result;
        }

        /// <summary>
        /// Clones a STARNET holon via <see cref="ISTARNETManagerBase{T1,T2,T3,T4}.CloneAsync"/>.
        /// <paramref name="options"/> may be a source id or name string (non-interactive argv); when null/empty in interactive mode, <see cref="FindAsync"/> prompts.
        /// </summary>
        public async Task<OASISResult<T1>> CloneAsync(object options = null)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string idOrName = options != null ? options.ToString()?.Trim() : null;
            if (string.IsNullOrEmpty(idOrName))
                idOrName = "";

            OASISResult<T1> findResult = await FindAsync("clone", idOrName, default, true, providerType: ProviderType.Default);
            if (findResult == null || findResult.IsError || findResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, findResult?.Message ?? $"Could not find {STARNETManager.STARNETHolonUIName} to clone.");
                return result;
            }

            T1 source = findResult.Result;
            if (source.STARNETDNA == null || source.STARNETDNA.Id == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Source holon has no STARNET DNA id; cannot clone.");
                return result;
            }

            string newName = null;
            if (!CLIEngine.NonInteractive)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to give the clone a custom name? (No = append \" - Clone\" to '{source.STARNETDNA.Name}')"))
                    newName = CLIEngine.GetValidInput($"New name for the cloned {STARNETManager.STARNETHolonUIName}:");
            }

            OASISResult<T1> cloneResult = await STARNETManager.CloneAsync(STAR.BeamedInAvatar.Id, source.STARNETDNA.Id, newName, ProviderType.Default);
            if (cloneResult != null && !cloneResult.IsError && cloneResult.Result != null)
            {
                if (!CLIEngine.JsonOutput)
                    CLIEngine.ShowSuccessMessage(cloneResult.Message ?? "Clone completed.");
                // JSON / non-interactive: avoid dumping the full holon UI to stdout (use follow-up show <id> if needed).
                if (!CLIEngine.NonInteractive && !CLIEngine.JsonOutput)
                    await ShowAsync(cloneResult.Result);
                return cloneResult;
            }

            if (cloneResult != null && !string.IsNullOrEmpty(cloneResult.Message))
                OASISErrorHandling.HandleError(ref result, cloneResult.Message);
            else
                OASISErrorHandling.HandleError(ref result, "Clone failed.");
            return result;
        }

        //TODO: Finish implementing later!
        //public OASISResult<T3> FindForProviderAndInstallIfNotInstalled(string operationName, string idOrName = "", bool showOnlyForCurrentAvatar = true, string STARNETHolonUIName = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T3> result = new OASISResult<T3>();
        //    OASISResult<T1> downloadedCelestialBodyDNA = STARCLI.CelestialBodiesMetaDataDNA.FindForProvider(operationName, idOrName, showOnlyForCurrentAvatar, STARNETHolonUIName: STARNETHolonUIName, providerType: providerType);

        //    if (downloadedCelestialBodyDNA != null && downloadedCelestialBodyDNA.Result != null && !downloadedCelestialBodyDNA.IsError)
        //    {
        //        OASISResult<bool> celestialBodyDNAInstalledResult = STAR.STARAPI.CelestialBodiesMetaDataDNA.IsInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //        if (celestialBodyDNAInstalledResult != null && !celestialBodyDNAInstalledResult.IsError)
        //        {
        //            if (!celestialBodyDNAInstalledResult.Result)
        //            {
        //                if (CLIEngine.GetConfirmation($"The selected {STARNETHolonUIName} is not currently installed. Do you wish to install it now?"))
        //                {
        //                    OASISResult<T3> installResult = DownloadAndInstall(downloadedCelestialBodyDNA.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //                    if (installResult.Result != null && !installResult.IsError)
        //                        result = installResult;
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"Error occured installing the {STARNETHolonUIName}. Reason: {installResult.Message}");
        //                }
        //            }
        //            else
        //            {
        //                OASISResult<T3> loadResult = STARNETManager.LoadInstalled(STAR.BeamedInAvatar.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.Id, downloadedCelestialBodyDNA.Result.STARNETDNA.VersionSequence, providerType);

        //                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
        //                    result = loadResult;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error occured loading the {STARNETHolonUIName}. Reason: {loadResult.Message}");
        //            }
        //        }
        //        else
        //            CLIEngine.ShowErrorMessage($"Error occured checking if {STARNETHolonUIName} is installed. Reason: {celestialBodyDNAInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding {STARNETHolonUIName}. Reason: {downloadedCelestialBodyDNA.Message}");

        //    return result;
        //}


        //private async Task<OASISResult<T1>> FindForProviderAndInstallAsync(string operationName, string downloadPath, string installPath, string idOrName = "", bool showOnlyForCurrentAvatar = true, bool addSpace = true, bool simpleWizard = true, InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<T1> result = new OASISResult<T1>();
        //    ProviderType largeFileProviderType = ProviderType.IPFSOASIS;


        //    //OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

        //    //if (result != null && result.Result != null && !result.IsError)
        //    //{
        //    //    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //    //        installMode = InstallMode.DownloadAndReInstall;

        //    //    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
        //    //}

        //    OASISResult<T1> templateResult = await FindForProviderAsync(operationName, idOrName, showOnlyForCurrentAvatar,addSpace, simpleWizard, providerType: providerType);

        //    if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
        //    {
        //        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
        //            installMode = InstallMode.DownloadAndReInstall;

        //        DownloadAndInstallAsync(idOrName, downloadPath, installPath, templateResult.Result, installMode, providerType);

        //        //OASISResult<bool> oappTemplateInstalledResult = await CheckIfInstalledAndInstallAsync(templateResult.Result, downloadPath, installPath, installMode, )

        //        //if (oappTemplateInstalledResult != null && !oappTemplateInstalledResult.IsError)
        //        //{
        //        //    if (!oappTemplateInstalledResult.Result)
        //        //    {
        //        //        if (CLIEngine.GetConfirmation($"The selected OAPP Template is not currently installed. Do you wish to install it now?"))
        //        //        {
        //        //            OASISResult<InstalledOAPPTemplate> installResult = await STARCLI.OAPPTemplates.DownloadAndInstallAsync(templateResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

        //        //            if (installResult.Result != null && !installResult.IsError)
        //        //            {
        //        //                templateInstalled = true;
        //        //                OAPPTemplate = installResult.Result;
        //        //            }
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        templateInstalled = true;
        //        //        OAPPTemplate = templateResult.Result;
        //        //    }
        //        //}
        //        //else
        //        //    CLIEngine.ShowErrorMessage($"Error occured checking if OAPP Template is installed. Reason: {oappTemplateInstalledResult.Message}");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured finding OAPP Template. Reason: {templateResult.Message}");


        //    return result;
        //}


    }
}
