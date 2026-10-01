using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar email from Holochain
                // Fallback: load all and filter by CreatedByEmail
                var allHolonsEmailResult = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                var holons = new OASISResult<IEnumerable<IHolon>> { Result = allHolonsEmailResult.Result?.Where(h => string.Equals(h.MetaData != null && h.MetaData.ContainsKey("CreatedByEmail") ? h.MetaData["CreatedByEmail"]?.ToString() : null, avatarEmailAddress, StringComparison.OrdinalIgnoreCase)) };
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarEmailAddress} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons from Holochain
                var holons = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
                {
                    ["loadChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["loadChildrenFromProvider"] = false.ToString()
                });

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Holo: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Holo: {ex.Message}", ex);
            }
            return result;
        } 

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeParams)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                // Parse nativeParams to get celestialBodyDNAFolder
                // Format: JSON string with "celestialBodyDNAFolder" or just the folder path string
                string celestialBodyDNAFolder = null;
                if (!string.IsNullOrEmpty(nativeParams))
                {
                    try
                    {
                        var paramsObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(nativeParams);
                        paramsObj?.TryGetValue("celestialBodyDNAFolder", out celestialBodyDNAFolder);
                    }
                    catch
                    {
                        // If not JSON, assume it's the folder path directly
                        celestialBodyDNAFolder = nativeParams;
                    }
                }

                // If no folder provided, try to get from celestialBody metadata or skip
                if (string.IsNullOrEmpty(celestialBodyDNAFolder))
                {
                    // Try to generate from celestialBody structure if available
                    if (celestialBody?.CelestialBodyCore?.Zomes != null && celestialBody.CelestialBodyCore.Zomes.Count > 0)
                    {
                        return GenerateRustFromCelestialBody(celestialBody, outputFolder);
                    }
                    return false;
                }

                // Ensure the Rust output folder exists for this OAPP.
                string rustFolder = Path.Combine(outputFolder, "Rust");
                if (!Directory.Exists(rustFolder))
                    Directory.CreateDirectory(rustFolder);

                // Get OASISDNA to access Rust template paths from HoloOASIS settings
                // Use injected OASISDNA or fallback to OASISBootLoader
                if (_oasisDNA == null || _oasisDNA.OASIS.StorageProviders?.HoloOASIS == null)
                    return false;

                var holoSettings = _oasisDNA.OASIS.StorageProviders.HoloOASIS;
                
                // Get base STAR path and Rust template folder from OASISDNA (JSON may use forward slashes; normalize for current OS)
                string starBasePath = NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.STARBasePath);
                string rustTemplateFolder = NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustDNARSMTemplateFolder);
                
                // Construct full path to Rust templates
                string baseSTARPathFull = string.IsNullOrEmpty(starBasePath) 
                    ? rustTemplateFolder  // If STARBasePath is empty, assume RustDNARSMTemplateFolder is absolute
                    : Path.Combine(starBasePath, rustTemplateFolder);

                if (!Directory.Exists(baseSTARPathFull))
                    return false;

                // Load Rust templates using paths from OASISDNA
                string libTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateLib)));
                string createTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateCreate)));
                string readTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateRead)));
                string updateTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateUpdate)));
                string deleteTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateDelete)));
                string validationTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateValidation)));
                string holonTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateHolon)));
                string intTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateInt)));
                string stringTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateString)));
                string boolTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateBool)));

                // Process DNA files to generate Rust code
                string libBuffer = "";
                string holonBufferRust = "";
                string holonFieldsClone = "";
                string holonName = "";
                string zomeName = "";
                int nextLineToWrite = 0;
                bool firstField = true;

                DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
                FileInfo[] files = dirInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    if (file == null) continue;

                    using (StreamReader reader = file.OpenText())
                    {
                        bool holonReached = false;

                        while (!reader.EndOfStream)
                        {
                            string buffer = reader.ReadLine();
                            if (string.IsNullOrEmpty(buffer)) continue;

                            if (buffer.Contains("ZomeDNA"))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 7)
                                {
                                    zomeName = parts[6].ToSnakeCase();
                                    libBuffer = libTemplate.Replace("zome_name", zomeName);
                                    nextLineToWrite = 0;
                                }
                            }

                            if (holonReached && (buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool")))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 15)
                                {
                                    string fieldName = parts[14].ToSnakeCase();
                                    string fieldType = parts[13].ToLower();

                                    string fieldTemplate = fieldType switch
                                    {
                                        "string" => stringTemplateRust,
                                        "int" => intTemplateRust,
                                        "bool" => boolTemplateRust,
                                        _ => null
                                    };

                                    if (fieldTemplate != null)
                                    {
                                        GenerateRustField(fieldName, fieldTemplate, holonName, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    }
                                }
                            }

                            // Write the holon out
                            if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                            {
                                if (holonBufferRust.Length > 2)
                                    holonBufferRust = holonBufferRust.Remove(holonBufferRust.Length - 3);

                                holonBufferRust = string.Concat(Environment.NewLine, holonBufferRust, Environment.NewLine, holonTemplateRust.Substring(holonTemplateRust.Length - 1, 1), Environment.NewLine);

                                int zomeIndex = libTemplate.IndexOf("#[zome]");
                                int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);

                                libBuffer = libBuffer.Insert(zomeIndex - 2, holonBufferRust);

                                if (nextLineToWrite == 0)
                                    nextLineToWrite = zomeBodyStartIndex + holonBufferRust.Length;
                                else
                                    nextLineToWrite += holonBufferRust.Length;

                                // Insert CRUD methods for each holon
                                string holonPascal = holonName.ToPascalCase();
                                string holonSnake = holonName.ToSnakeCase();
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake).Replace("//#CopyFields//", holonFieldsClone), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));

                                holonBufferRust = "";
                                holonFieldsClone = "";
                                holonReached = false;
                                firstField = true;
                                holonName = "";
                            }

                            if (buffer.Contains("HolonDNA"))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 11)
                                {
                                    holonName = parts[10].ToSnakeCase();
                                    holonBufferRust = holonTemplateRust.Replace("Holon", parts[10].ToPascalCase()).Replace("{holon}", holonName);
                                    holonBufferRust = holonBufferRust.Substring(0, holonBufferRust.Length - 1);
                                    holonReached = true;
                                    firstField = true;
                                }
                            }
                        }
                    }
                    nextLineToWrite = 0;
                }

                // Write the generated Rust lib.rs file
                if (!string.IsNullOrEmpty(libBuffer))
                {
                    File.WriteAllText(Path.Combine(rustFolder, "lib.rs"), libBuffer);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Log error if logging available
                return false;
            }
        }

        private void GenerateRustField(string fieldName, string fieldTemplate, string holonName, ref bool firstField, ref string holonFieldsClone, ref string holonBufferRust)
        {
            if (firstField)
                firstField = false;
            else
                holonFieldsClone = string.Concat(holonFieldsClone, "\t");

            holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
            holonBufferRust = string.Concat(holonBufferRust, fieldTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
        }

        private bool GenerateRustFromCelestialBody(ICelestialBody celestialBody, string outputFolder)
        {
            try
            {
                if (_oasisDNA?.OASIS?.StorageProviders?.HoloOASIS == null)
                    return false;

                var holoSettings = _oasisDNA.OASIS.StorageProviders.HoloOASIS;
                string starBasePath = NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.STARBasePath);
                string rustTemplateFolder = NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustDNARSMTemplateFolder);
                string baseSTARPathFull = string.IsNullOrEmpty(starBasePath)
                    ? rustTemplateFolder
                    : Path.Combine(starBasePath, rustTemplateFolder);

                if (!Directory.Exists(baseSTARPathFull))
                    return false;

                string libTemplate        = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateLib)));
                string createTemplate     = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateCreate)));
                string readTemplate       = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateRead)));
                string updateTemplate     = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateUpdate)));
                string deleteTemplate     = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateDelete)));
                string validationTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateValidation)));
                string holonTemplateRust  = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateHolon)));
                string intTemplateRust    = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateInt)));
                string stringTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateString)));
                string boolTemplateRust   = File.ReadAllText(Path.Combine(baseSTARPathFull, NextGenSoftware.Utilities.PathHelper.NormalizePathFromConfig(holoSettings.RustTemplateBool)));

                string rustFolder = Path.Combine(outputFolder, "Rust");
                if (!Directory.Exists(rustFolder))
                    Directory.CreateDirectory(rustFolder);

                // Collect IHolonBase property names so we skip them during reflection
                var basePropertyNames = typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase)
                    .GetProperties().Select(p => p.Name).ToHashSet();

                string libBuffer = "";
                bool anyGenerated = false;

                foreach (IZome zome in celestialBody.CelestialBodyCore.Zomes)
                {
                    string zomeName = (zome.Name ?? "zome").ToSnakeCase();
                    libBuffer = libTemplate.Replace("zome_name", zomeName);
                    int nextLineToWrite = 0;

                    var holonsResult = zome.LoadChildHolons();
                    var holons = holonsResult?.Result ?? Enumerable.Empty<IHolon>();

                    foreach (IHolon holon in holons)
                    {
                        string holonNameSnake  = (holon.Name ?? "holon").ToSnakeCase();
                        string holonNamePascal = (holon.Name ?? "Holon").ToPascalCase();

                        string holonBufferRust = holonTemplateRust
                            .Replace("Holon", holonNamePascal)
                            .Replace("{holon}", holonNameSnake);
                        holonBufferRust = holonBufferRust.Substring(0, holonBufferRust.Length - 1);

                        string holonFieldsClone = "";
                        bool firstField = true;

                        // Reflect on the concrete holon type; emit only string/int/bool domain properties
                        foreach (var prop in holon.GetType().GetProperties()
                            .Where(p => p.CanRead && !basePropertyNames.Contains(p.Name)))
                        {
                            string fieldTemplate = null;
                            if (prop.PropertyType == typeof(string))
                                fieldTemplate = stringTemplateRust;
                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(long) || prop.PropertyType == typeof(uint))
                                fieldTemplate = intTemplateRust;
                            else if (prop.PropertyType == typeof(bool))
                                fieldTemplate = boolTemplateRust;

                            if (fieldTemplate != null)
                                GenerateRustField(prop.Name.ToSnakeCase(), fieldTemplate, holonNameSnake, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                        }

                        if (holonBufferRust.Length > 2)
                            holonBufferRust = holonBufferRust.Remove(holonBufferRust.Length - 3);

                        holonBufferRust = string.Concat(Environment.NewLine, holonBufferRust, Environment.NewLine, holonTemplateRust.Substring(holonTemplateRust.Length - 1, 1), Environment.NewLine);

                        int zomeIndex         = libTemplate.IndexOf("#[zome]");
                        int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);
                        libBuffer = libBuffer.Insert(zomeIndex - 2, holonBufferRust);

                        if (nextLineToWrite == 0)
                            nextLineToWrite = zomeBodyStartIndex + holonBufferRust.Length;
                        else
                            nextLineToWrite += holonBufferRust.Length;

                        libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("Holon", holonNamePascal).Replace("{holon}", holonNameSnake), Environment.NewLine));
                        libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("Holon", holonNamePascal).Replace("{holon}", holonNameSnake), Environment.NewLine));
                        libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("Holon", holonNamePascal).Replace("{holon}", holonNameSnake).Replace("//#CopyFields//", holonFieldsClone), Environment.NewLine));
                        libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("Holon", holonNamePascal).Replace("{holon}", holonNameSnake), Environment.NewLine));
                        libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("Holon", holonNamePascal).Replace("{holon}", holonNameSnake), Environment.NewLine));

                        anyGenerated = true;
                    }
                }

                if (!anyGenerated || string.IsNullOrEmpty(libBuffer))
                    return false;

                File.WriteAllText(Path.Combine(rustFolder, "lib.rs"), libBuffer);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }



    }
}
