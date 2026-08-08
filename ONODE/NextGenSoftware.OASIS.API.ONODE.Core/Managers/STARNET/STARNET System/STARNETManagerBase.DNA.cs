using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4> where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public virtual async Task<OASISResult<bool>> WriteDNAAsync<T>(T STARNETDNA, string fullPathToSTARNETHolon) //where T : ISTARNETDNA
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(fullPathToSTARNETHolon))
                {
                    result.Result = true;
                    return result;
                }

                //JsonSerializerOptions options = new()
                //{
                //    ReferenceHandler = ReferenceHandler.Preserve,
                //    WriteIndented = true
                //};

                if (!Directory.Exists(fullPathToSTARNETHolon))
                    Directory.CreateDirectory(fullPathToSTARNETHolon);

                OAPPDNA OAPPDNA = STARNETDNA as OAPPDNA;

                //Temp need to remove CelestialBody parents and cores to prevent infinite recursion when serializing to json.
                //if (OAPPDNA != null)
                //    Data.RemoveCelesialBodies(OAPPDNA.Zomes);

                await File.WriteAllTextAsync(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                //await File.WriteAllTextAsync(Path.Combine(fullPathToSTARNETHolon, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version)), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));

                //Restore the celestial bodies & cores back onto the zomes.
                //if (OAPPDNA != null)
                //    Data.RestoreCelesialBodies(OAPPDNA.Zomes);

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARNETHolonUIName} DNA in WriteDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> WriteDNA<T>(T STARNETDNA, string fullPathToSTARNETHolon) //where T : ISTARNETDNA
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(fullPathToSTARNETHolon))
                {
                    result.Result = true;
                    return result;
                }

                if (!Directory.Exists(fullPathToSTARNETHolon))
                    Directory.CreateDirectory(fullPathToSTARNETHolon);

                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonSerializer.Serialize(STARNETDNA));
                File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, STARNETDNAFileName), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                //File.WriteAllText(Path.Combine(fullPathToSTARNETHolon, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version)), JsonConvert.SerializeObject(STARNETDNA, Formatting.Indented));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {STARNETHolonUIName} DNA in WriteDNA: Reason: {ex.Message}");
            }

            return result;
        }

        //public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(T STARNETDNA, string fullPathToSTARNETHolonFolder) where T : ISTARNETDNA
        public virtual async Task<OASISResult<T>> ReadDNAFromSourceOrInstallFolderAsync<T>(string fullPathToSTARNETHolonFolder)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //var options = new JsonSerializerOptions
                //{
                //    WriteIndented = true,
                //    Converters = 
                //    {
                //        //new InterfaceConverter<IList<STARNETDependency>, List<STARNETDependency>>(),
                //        //new InterfaceConverter<STARNETDependency, STARNETDependency>(),
                //        //new PolymorphicConverter<STARNETDependency>(),
                //        new STARNETDependencyConvertor()
                //    }
                //};

                result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                //result.Result = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(fullPathToSTARNETDNA));

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)), options);
                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolderAsync: Reason: {ex.Message}");
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETHolonFolder)
        //public OASISResult<T> ReadDNAFromSourceOrInstallFolder<T>(string fullPathToSTARNETDNA)
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPathToSTARNETDNA));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(fullPathToSTARNETHolonFolder, STARNETDNAFileName)));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToSTARNETHolonFolder} folder in ReadDNAFromSourceOrInstallFolder: Reason: {ex.Message}");
            }

            return result;
        }

        public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(string fullPathToPublishedFile)
        //public virtual async Task<OASISResult<T>> ReadDNAFromPublishedFileAsync<T>(T STARNETDNA, string fullPathToPublishedFile) where T : ISTARNETDNA
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                //result.Result = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        //public OASISResult<T> ReadDNAFromPublishedFile<T>(string fullPathToPublishedFile)
        {
            OASISResult<T> result = new OASISResult<T>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                //result.Result = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, STARNETDNAFileName)));
                //result.Result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(tempPath, string.Concat(Enum.GetName(typeof(HolonType), STARNETDNA.STARNETHolonType), "_", STARNETDNA.Name, "_", "v", STARNETDNA.Version))));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {STARNETDNAFileName} in the {fullPathToPublishedFile} file in ReadSTARNETDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }

        public OASISResult<bool> ValidateVersion(string dnaVersion, string storedVersion, string fullPathToSTARNETHolonFolder, bool firstPublish, bool edit)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            int dnaVersionInt = 0;
            int stotedVersionInt = 0;

            if (!firstPublish)
            {
                if (edit && dnaVersion != storedVersion)
                {
                    OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not the same as the version you are attempting to edit (v{storedVersion}). They must be the same if you wish to upload new files for version v{storedVersion}. Please edit the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                    return result;
                }
                else
                {
                    if (!StringHelper.IsValidVersion(dnaVersion))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (dnaVersion == storedVersion)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is the same as the previous version ({storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    if (!int.TryParse(dnaVersion.Replace(".", ""), out dnaVersionInt))
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is not valid! Please make sure you enter a valid version in the form of MM.mm.rr (Major.Minor.Revision) in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder ({fullPathToSTARNETHolonFolder}).");
                        return result;
                    }

                    //Should hopefully never occur! ;-)
                    if (!int.TryParse(storedVersion.Replace(".", ""), out stotedVersionInt))
                        OASISErrorHandling.HandleWarning(ref result, $"The version stored in the OASIS (v{storedVersion}) is not valid!");

                    if (dnaVersionInt <= stotedVersionInt)
                    {
                        OASISErrorHandling.HandleError(ref result, $"The version in the {STARNETHolonUIName} DNA (v{dnaVersion}) is less than the previous version (v{storedVersion}). Please make sure you increment the version in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder.");
                        return result;
                    }
                }
            }
            else if (dnaVersion != "1.0.0")
            {
                OASISErrorHandling.HandleError(ref result, $"The first version has to be 1.0.0! Please correct in the {STARNETDNAFileName} file found in the root of your {STARNETHolonUIName} folder.");
                return result;
            }

            result.Result = true;
            return result;
        }

    }
}