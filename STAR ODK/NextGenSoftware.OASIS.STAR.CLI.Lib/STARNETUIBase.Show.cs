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
        where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
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

    }
}