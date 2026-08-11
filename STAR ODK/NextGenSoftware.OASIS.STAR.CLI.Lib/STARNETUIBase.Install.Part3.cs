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
        
        /// <summary>STARNET DNA category can deserialize as <see cref="JsonElement"/> or other CLR types; avoid printing <c>ValueKind=...</c> to the user.</summary>
        private static string FormatStarnetDnaCategoryForDisplay(object? raw)
        {
            if (raw == null) return "None";
            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        return je.GetString() ?? "None";
                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out var i32))
                            return i32.ToString(CultureInfo.InvariantCulture);
                        if (je.TryGetInt64(out var i64))
                            return i64.ToString(CultureInfo.InvariantCulture);
                        return je.GetRawText();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "None";
                    default:
                        return je.GetRawText();
                }
            }

            var s = raw.ToString();
            if (string.IsNullOrEmpty(s))
                return "None";

            // Some JSON deserialization/serialization paths end up stringifying JsonElement
            // as an object like `{ "ValueKind": 3 }` (losing the actual value).
            // Convert this into a readable JsonValueKind name instead of printing the blob.
            try
            {
                if (s.Contains("\"ValueKind\"", StringComparison.Ordinal) && s.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ValueKind", out JsonElement vk))
                    {
                        if (vk.ValueKind == JsonValueKind.Number && vk.TryGetInt32(out int i))
                            return ((JsonValueKind)i).ToString();
                        if (vk.ValueKind == JsonValueKind.String)
                            return vk.GetString() ?? "None";
                    }
                }
            }
            catch
            {
                // Fall through to raw string output.
            }

            return s;
        }

        private T1 ConvertFromT3ToT1(T3 holon)
        {
            T1 newHolon = new T1();
            newHolon.STARNETDNA = holon.STARNETDNA;
            newHolon.MetaData = holon.MetaData;
            return newHolon;
        }

        private void OnPublishStatusChanged(object sender, STARNETHolonPublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonPublishStatus.DotNetPublishing:
                    CLIEngine.ShowWorkingMessage("DotNet Publishing...");
                    break;

                case STARNETHolonPublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonPublishStatus.Published:
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Published Successfully");
                    break;

                case STARNETHolonPublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonPublishStatus), e.Status)}...");
                    break;
            }
        }

        private void OnUploadStatusChanged(object sender, STARNETHolonUploadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private void OnInstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case STARNETHolonInstallStatus.Downloading:
                    CLIEngine.ShowMessage($"Downloading {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    Console.WriteLine("");
                    break;

                case STARNETHolonInstallStatus.Installing:
                    CLIEngine.ShowWorkingMessage($"Installing {e.STARNETDNA.Name} v{e.STARNETDNA.Version}...");
                    break;

                case STARNETHolonInstallStatus.InstallingDependencies:
                    CLIEngine.ShowWorkingMessage("Installing Dependencies (Smartbricks)...");
                    break;

                case STARNETHolonInstallStatus.InstallingRuntimes:
                    CLIEngine.ShowWorkingMessage("Installing Runtimes...");
                    break;

                case STARNETHolonInstallStatus.InstallingLibs:
                    CLIEngine.ShowWorkingMessage("Installing Libs...");
                    break;

                case STARNETHolonInstallStatus.InstallingTemplates:
                    CLIEngine.ShowWorkingMessage("Installing Templates...");
                    break;

                case STARNETHolonInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage($"{e.STARNETDNA.Name} v{e.STARNETDNA.Version} Installed Successfully");
                    break;

                case STARNETHolonInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(STARNETHolonInstallStatus), e.Status)}...");
                    break;
            }
        }

        private void OnDownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e)
        {
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }
    }
}
