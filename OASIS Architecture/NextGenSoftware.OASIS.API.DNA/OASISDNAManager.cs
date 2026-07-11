using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.DNA
{
    public static class OASISDNAManager
    {
        private static string SYSTEMOASISDNAPath = "OASIS_DNA_SYSTEM.json";
        public static string OASISDNAPath = "OASIS_DNA.json";
        public static OASISDNA OASISDNA { get; set; }

        /// <summary>Full path to OASIS_DNA.json after the last successful <see cref="LoadDNA(string)"/> (for single-file apps where CWD/BaseDirectory differ from the publish folder).</summary>
        public static string LastResolvedOASISDnaPhysicalPath { get; private set; }

        /// <summary>When install path (e.g. /usr/local/bin/DNA/) is read-only, resolve to user config path so load/save succeed.</summary>
        private static string ResolveEffectiveOASISDNAPath(string requestedPath)
        {
            if (string.IsNullOrWhiteSpace(requestedPath)) return requestedPath;
            string appRoot = AppPathHelper.ResolveAppRootDirectory();
            string fullPath = Path.IsPathRooted(requestedPath)
                ? Path.GetFullPath(requestedPath)
                : Path.GetFullPath(Path.Combine(appRoot, requestedPath.Replace('\\', Path.DirectorySeparatorChar)));
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && AppPathHelper.IsDirectoryWritable(dir))
                return fullPath;
            string userPath = Path.Combine(GetUserDNADirectory(), "OASIS_DNA.json");
            try
            {
                if (File.Exists(fullPath) && !File.Exists(userPath))
                    File.Copy(fullPath, userPath);
            }
            catch { /* non-fatal */ }
            return userPath;
        }

        private static string GetUserDNADirectory()
        {
            return AppPathHelper.GetUserDataSubDirectory("oasis-star-cli", "DNA");
        }

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
                else
                {
                    string effectivePath = ResolveEffectiveOASISDNAPath(OASISDNAPath);
                    if (!File.Exists(effectivePath))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");
                    else
                    {
                        OASISDNAManager.OASISDNAPath = effectivePath;

                        using (StreamReader r = new StreamReader(effectivePath))
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
                            RecordLastResolvedOasisDnaPath(effectivePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}");
            }

            return result;
        }

        private static void RecordLastResolvedOasisDnaPath(string pathUsedForOpen)
        {
            try
            {
                if (string.IsNullOrEmpty(pathUsedForOpen))
                    return;
                LastResolvedOASISDnaPhysicalPath = Path.GetFullPath(
                    Path.IsPathRooted(pathUsedForOpen)
                        ? pathUsedForOpen
                        : Path.Combine(Environment.CurrentDirectory, pathUsedForOpen.Replace('\\', Path.DirectorySeparatorChar)));
            }
            catch
            {
                // non-fatal
            }
        }

        public static async Task<OASISResult<OASISDNA>> LoadDNAAsync(string OASISDNAPath)
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            string errorMessage = "Error occured in OASISDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveOASISDNAPath(OASISDNAPath);
                    if (!File.Exists(effectivePath))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");
                    else
                    {
                        OASISDNAManager.OASISDNAPath = effectivePath;

                        using (StreamReader r = new StreamReader(effectivePath))
                        {
                            string json = await r.ReadToEndAsync();
                            OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                            result.Result = OASISDNA;
                            RecordLastResolvedOasisDnaPath(effectivePath);
                        }
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

        /// <summary>If candidate's directory is not writable (e.g. install path), return user path and copy file if needed.</summary>
        private static string EnsureWritableOASISDNAPath(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return candidate;
            string dir = Path.GetDirectoryName(candidate);
            if (!string.IsNullOrEmpty(dir) && AppPathHelper.IsDirectoryWritable(dir))
                return candidate;
            string userPath = Path.Combine(GetUserDNADirectory(), "OASIS_DNA.json");
            try
            {
                if (File.Exists(candidate) && !File.Exists(userPath))
                    File.Copy(candidate, userPath);
            }
            catch { /* non-fatal */ }
            return userPath;
        }

        private static string ResolveOasisDnaPhysicalPath()
        {
            // Prefer DNA next to the real executable (publish/linux-x64/DNA/). Single-file extract dirs
            // under /tmp/.net/... are ephemeral — SecretKey must persist beside star, not in extract.
            string candidate = null;
            try
            {
                string proc = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(proc))
                {
                    string starDir = Path.GetDirectoryName(proc);
                    if (!string.IsNullOrEmpty(starDir))
                    {
                        string nextToStar = Path.Combine(starDir, "DNA", "OASIS_DNA.json");
                        if (File.Exists(nextToStar))
                            candidate = Path.GetFullPath(nextToStar);
                    }
                }
            }
            catch { /* non-fatal */ }

            if (string.IsNullOrEmpty(candidate) && !string.IsNullOrEmpty(LastResolvedOASISDnaPhysicalPath) && File.Exists(LastResolvedOASISDnaPhysicalPath))
                candidate = LastResolvedOASISDnaPhysicalPath;

            if (string.IsNullOrEmpty(candidate))
            {
                string rel = string.IsNullOrWhiteSpace(OASISDNAPath)
                    ? Path.Combine("DNA", "OASIS_DNA.json")
                    : OASISDNAPath.Replace('\\', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(rel) && File.Exists(rel))
                    candidate = Path.GetFullPath(rel);
                if (string.IsNullOrEmpty(candidate))
                {
                    string fromCwd = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, rel));
                    candidate = File.Exists(fromCwd) ? fromCwd : null;
                }
                if (string.IsNullOrEmpty(candidate))
                {
                    string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string fromExe = Path.GetFullPath(Path.Combine(baseDir, rel));
                    candidate = File.Exists(fromExe) ? fromExe : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, rel));
                }
            }

            return EnsureWritableOASISDNAPath(candidate);
        }

        private static async Task<OASISResult<bool>> PersistSecretKeyWithJObjectAsync(string oasisdnaPath, string newSecretKey)
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
                JObject jo = JObject.Parse(content);
                JObject oasis = jo["OASIS"] as JObject;
                if (oasis == null)
                {
                    OASISErrorHandling.HandleError(ref result, "OASIS_DNA.json has no OASIS root object.");
                    return result;
                }

                JObject sec = oasis["Security"] as JObject;
                if (sec == null)
                {
                    sec = new JObject();
                    oasis["Security"] = sec;
                }

                sec["SecretKey"] = newSecretKey;
                await File.WriteAllTextAsync(oasisdnaPath, jo.ToString(Formatting.Indented));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"PersistSecretKeyWithJObjectAsync: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Ensures JWT SecretKey is set on <see cref="OASISDNA"/> and written to disk (regex patch or JObject fallback).
        /// Reloads from disk if the static DNA instance is null. Call before beam-in / JWT issuance.
        /// </summary>
        public static OASISResult<bool> EnsureJwtSecretKeyReadyForAvatarAuth()
        {
            return EnsureJwtSecretKeyReadyForAvatarAuthAsync().GetAwaiter().GetResult();
        }

        public static async Task<OASISResult<bool>> EnsureJwtSecretKeyReadyForAvatarAuthAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            const string err = "OASISDNAManager.EnsureJwtSecretKeyReadyForAvatarAuthAsync. Reason: ";
            try
            {
                string path = ResolveOasisDnaPhysicalPath();
                if (!File.Exists(path))
                {
                    OASISErrorHandling.HandleError(ref result, $"{err}OASIS DNA file not found. Tried: {path}");
                    return result;
                }

                if (OASISDNA == null)
                {
                    OASISResult<OASISDNA> loadResult = await LoadDNAAsync(path);
                    if (loadResult == null || loadResult.IsError || loadResult.Result == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{err}{loadResult?.Message ?? "LoadDNA failed."}");
                        return result;
                    }
                }

                if (OASISDNA.OASIS == null)
                    OASISDNA.OASIS = new OASIS();
                if (OASISDNA.OASIS.Security == null)
                    OASISDNA.OASIS.Security = new SecuritySettings();

                if (string.IsNullOrWhiteSpace(OASISDNA.OASIS.Security.SecretKey))
                    OASISDNA.OASIS.Security.SecretKey = string.Concat(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));

                OASISDNAPath = path;

                OASISResult<bool> patchResult = await TryUpdateSecretKeyInFileAsync(path, OASISDNA.OASIS.Security.SecretKey);
                if (!patchResult.IsError && patchResult.Result)
                {
                    result.Result = true;
                    return result;
                }

                OASISResult<bool> jResult = await PersistSecretKeyWithJObjectAsync(path, OASISDNA.OASIS.Security.SecretKey);
                if (jResult.IsError || !jResult.Result)
                {
                    result.Result = true;
                    result.IsWarning = true;
                    result.Message = string.IsNullOrEmpty(jResult.Message)
                        ? "SecretKey generated in memory; could not write OASIS_DNA.json (read-only or permission)."
                        : jResult.Message;
                    return result;
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{err}{ex.Message}");
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
                else if (OASISDNA == null)
                    OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.SaveDNA. Reason: OASISDNA cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveOASISDNAPath(OASISDNAPath);
                    string dir = Path.GetDirectoryName(effectivePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = effectivePath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    using (var writer = new StreamWriter(effectivePath))
                        writer.Write(json);
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
                else if (OASISDNA == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNA cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveOASISDNAPath(OASISDNAPath);
                    string dir = Path.GetDirectoryName(effectivePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = effectivePath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    using (var writer = new StreamWriter(effectivePath))
                        await writer.WriteAsync(json);
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