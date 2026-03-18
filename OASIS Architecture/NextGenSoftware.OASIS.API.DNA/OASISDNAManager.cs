using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.DNA
{
    public static class OASISDNAManager
    {
        private static string SYSTEMOASISDNAPath = "OASIS_DNA_SYSTEM.json";
        public static string OASISDNAPath = "OASIS_DNA.json";
        public static OASISDNA OASISDNA { get; set; }

        private static OASISResult<OASISDNA> GetSystemOASISDNA()
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            result.Result = JsonConvert.DeserializeObject<OASISDNA>(FileEncryption.DecryptFile(SYSTEMOASISDNAPath, DNALoader.DNALoader.GetSystemOASISDNAKey(), DNALoader.DNALoader.GetSystemOASISDNAIV()));
            return result;
        }

        public static OASISResult<OASISDNA> LoadDNA()
        {
            return LoadDNA(OASISDNAPath);
        }

        public static async Task<OASISResult<OASISDNA>> LoadDNAAsync()
        {
            return await LoadDNAAsync(OASISDNAPath);
        }

        public static OASISResult<OASISDNA> LoadDNA(string OASISDNAPath)
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            string errorMessage = "Error occured in OASISDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");
                
                else
                {
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    using (StreamReader r = new StreamReader(OASISDNAPath))
                    {
                        string json = r.ReadToEnd();
                        OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);

                        //OASISResult<OASISDNA> OASISDNAResult = GetSystemOASISDNA();

                        //if (OASISDNAResult != null && OASISDNAResult.Result != null && !OASISDNAResult.IsError)
                        //{
                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.EmailFrom))
                        //        OASISDNA.OASIS.Email.EmailFrom = OASISDNAResult.Result.OASIS.Email.EmailFrom;

                        //    if (OASISDNA.OASIS.Email.SmtpPort == 0)
                        //        OASISDNA.OASIS.Email.SmtpPort = OASISDNAResult.Result.OASIS.Email.SmtpPort;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpPass))
                        //        OASISDNA.OASIS.Email.SmtpPass = OASISDNAResult.Result.OASIS.Email.SmtpPass;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpHost))
                        //        OASISDNA.OASIS.Email.SmtpHost = OASISDNAResult.Result.OASIS.Email.SmtpHost;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpUser))
                        //        OASISDNA.OASIS.Email.SmtpHost = OASISDNAResult.Result.OASIS.Email.SmtpUser;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.OASISWebSiteURL))
                        //        OASISDNA.OASIS.Email.OASISWebSiteURL = OASISDNAResult.Result.OASIS.Email.OASISWebSiteURL;

                        //    //TODO: Finish implementing for the rest of the properties! :)

                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.Provider))
                        //    //    OASISDNA.OASIS.Provider = OASISDNAResult.Result.OASIS.Provider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.DefaultStorageProvider))
                        //    //    OASISDNA.OASIS.DefaultStorageProvider = OASISDNAResult.Result.OASIS.DefaultStorageProvider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.FallbackStorageProvider))
                        //    //    OASISDNA.OASIS.FallbackStorageProvider = OASISDNAResult.Result.OASIS.FallbackStorageProvider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.AvailableProviders))
                        //    //    OASISDNA.OASIS.AvailableProviders = OASISDNAResult.Result.OASIS.AvailableProviders;
                        //}

                        //if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.EmailFrom))
                        //    OASISDNA.OASIS.Email.EmailFrom = GetSystemOASISDNA().Result.OASIS.Email.EmailFrom;

                        //if (OASISDNA.OASIS.Email.SmtpPort == 0)
                        //    OASISDNA.OASIS.Email.SmtpPort = GetSystemOASISDNA().Result.OASIS.Email.SmtpPort;

                        //if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpPass))
                        //    OASISDNA.OASIS.Email.SmtpPass = GetSystemOASISDNA().Result.OASIS.Email.SmtpPass;

                        result.Result = OASISDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<OASISDNA>> LoadDNAAsync(string OASISDNAPath)
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            string errorMessage = "Error occured in OASISDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    using (StreamReader r = new StreamReader(OASISDNAPath))
                    {
                        string json = await r.ReadToEndAsync();
                        OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                        result.Result = OASISDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// If <see cref="SecuritySettings.SecretKey"/> is missing, generates one from two GUIDs (JWT signing)
        /// and persists to the OASIS DNA JSON file when it exists on disk. Safe to call every boot.
        /// </summary>
        public static OASISResult<bool> EnsureSecuritySecretKeyPersisted(string preferredSavePath = null)
        {
            return EnsureSecuritySecretKeyPersistedAsync(preferredSavePath).GetAwaiter().GetResult();
        }

        public static async Task<OASISResult<bool>> EnsureSecuritySecretKeyPersistedAsync(string preferredSavePath = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            const string errorMessage = "OASISDNAManager.EnsureSecuritySecretKeyPersistedAsync. Reason: ";

            try
            {
                if (OASISDNA == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNA is not loaded.");
                    return result;
                }

                if (OASISDNA.OASIS == null)
                    OASISDNA.OASIS = new OASIS();
                if (OASISDNA.OASIS.Security == null)
                    OASISDNA.OASIS.Security = new SecuritySettings();

                if (!string.IsNullOrWhiteSpace(OASISDNA.OASIS.Security.SecretKey))
                {
                    result.Result = true;
                    return result;
                }

                OASISDNA.OASIS.Security.SecretKey = string.Concat(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));

                string savePath = preferredSavePath ?? OASISDNAPath;
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    result.Result = true;
                    result.Message = "OASIS Security.SecretKey generated for this session only (no DNA path).";
                    return result;
                }

                if (!Path.IsPathRooted(savePath))
                    savePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, savePath.Replace('\\', Path.DirectorySeparatorChar)));

                if (!File.Exists(savePath))
                {
                    result.Result = true;
                    result.IsWarning = true;
                    result.Message = "OASIS Security.SecretKey generated in memory; DNA file not found to persist — add SecretKey to OASIS_DNA.json or place the file at the expected path.";
                    return result;
                }

                OASISDNAManager.OASISDNAPath = preferredSavePath ?? OASISDNAPath;

                // Update only SecretKey in the file so comments and formatting in OASIS_DNA.json are preserved.
                OASISResult<bool> patchResult = await TryUpdateSecretKeyInFileAsync(savePath, OASISDNA.OASIS.Security.SecretKey);
                if (patchResult.IsError || !patchResult.Result)
                {
                    result.Result = true;
                    result.IsWarning = true;
                    result.Message = string.IsNullOrEmpty(patchResult.Message)
                        ? "OASIS Security.SecretKey is set for this session but could not be written to the file (SecretKey property not found or invalid format)."
                        : patchResult.Message;
                    return result;
                }

                result.Result = true;
                result.Message = "Generated and saved OASIS Security.SecretKey (fresh install).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Updates only the OASIS.Security.SecretKey value in the JSON file so that comments and formatting are preserved.
        /// Call this when persisting a newly generated SecretKey; do not use full SaveDNA for that case or comments will be lost.
        /// </summary>
        private static async Task<OASISResult<bool>> TryUpdateSecretKeyInFileAsync(string oasisdnaPath, string newSecretKey)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (string.IsNullOrEmpty(oasisdnaPath) || !File.Exists(oasisdnaPath))
                {
                    OASISErrorHandling.HandleError(ref result, "OASIS DNA file path is missing or file does not exist.");
                    return result;
                }

                string content = await File.ReadAllTextAsync(oasisdnaPath);

                // Match "SecretKey" : "" or "SecretKey": "value" or "SecretKey" : null (inside OASIS.Security). Replace value only.
                // Escaped new key for JSON: backslash and quote must be escaped.
                string escapedKey = newSecretKey.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string replacement = $"\"SecretKey\": \"{escapedKey}\"";

                const string pattern = @"""SecretKey""\s*:\s*(?:""[^""]*""|null)";
                string newContent = Regex.Replace(content, pattern, replacement);

                if (newContent == content)
                {
                    result.Result = false;
                    result.Message = "SecretKey property not found or already set in OASIS_DNA.json; file unchanged.";
                    return result;
                }

                await File.WriteAllTextAsync(oasisdnaPath, newContent);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating SecretKey in OASIS DNA file: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> SaveDNA()
        {
            return SaveDNA(OASISDNAPath, OASISDNA);
        }

        public static OASISResult<bool> SaveDNA(string OASISDNAPath, OASISDNA OASISDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OASISDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    if (OASISDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.SaveDNA. Reason: OASISDNA cannot be null.");

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    StreamWriter writer = new StreamWriter(OASISDNAPath);
                    writer.Write(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync()
        {
            return await SaveDNAAsync(OASISDNAPath, OASISDNA);
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync(string OASISDNAPath, OASISDNA OASISDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OASISDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    if (OASISDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNA cannot be null.");

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    StreamWriter writer = new StreamWriter(OASISDNAPath);
                    await writer.WriteAsync(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }
    }
}