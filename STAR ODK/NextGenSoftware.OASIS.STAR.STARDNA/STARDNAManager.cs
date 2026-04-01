using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    /// <summary>
    /// Manages STAR DNA: load/save of STAR_DNA.json, runtime path resolution, and install/read-only handling.
    /// <see cref="STARDNA"/> is configuration only; shared generic path/file helpers are centralized in <see cref="AppPathHelper"/>.
    /// </summary>
    public static class STARDNAManager
    {
        public static string STARDNAPath = Path.Combine("DNA", "STARDNA.json");
        public static STARDNA STARDNA { get; set; }

        /// <summary>When install path (e.g. /usr/local/bin/DNA/) is read-only, resolve to user config path so load/save succeed.</summary>
        private static string ResolveEffectiveSTARDNAPath(string requestedPath)
        {
            if (string.IsNullOrWhiteSpace(requestedPath)) return requestedPath;
            string appRoot = AppPathHelper.ResolveAppRootDirectory();
            string fullPath = Path.IsPathRooted(requestedPath)
                ? Path.GetFullPath(requestedPath)
                : Path.GetFullPath(Path.Combine(appRoot, requestedPath.Replace('\\', Path.DirectorySeparatorChar)));
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && AppPathHelper.IsDirectoryWritable(dir))
                return fullPath;
            string fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "STAR_DNA.json";
            string userPath = Path.Combine(GetUserDNADirectory(), fileName);
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

        public static OASISResult<STARDNA> LoadDNA()
        {
            return LoadDNA(STARDNAPath);
        }

        public static async Task<OASISResult<STARDNA>> LoadDNAAsync()
        {
            return await LoadDNAAsync(STARDNAPath);
        }

        public static OASISResult<STARDNA> LoadDNA(string STARDNAPath)
        {
            OASISResult<STARDNA> result = new OASISResult<STARDNA>();
            string errorMessage = "Error occured in STARDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveSTARDNAPath(STARDNAPath);
                    if (!File.Exists(effectivePath))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STAR DNA file.");
                    else
                    {
                        STARDNAManager.STARDNAPath = effectivePath;

                        using (StreamReader r = new StreamReader(effectivePath))
                        {
                            string json = r.ReadToEnd();
                            STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                            if (STARDNA != null)
                                ResolveRuntimeBasePaths(STARDNA);
                            result.Result = STARDNA;
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

        public static async Task<OASISResult<STARDNA>> LoadDNAAsync(string STARDNAPath)
        {
            OASISResult<STARDNA> result = new OASISResult<STARDNA>();
            string errorMessage = "Error occured in STARDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveSTARDNAPath(STARDNAPath);
                    if (!File.Exists(effectivePath))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STAR DNA file.");
                    else
                    {
                        STARDNAManager.STARDNAPath = effectivePath;

                        using (StreamReader r = new StreamReader(effectivePath))
                        {
                            string json = await r.ReadToEndAsync();
                            STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                            if (STARDNA != null)
                                ResolveRuntimeBasePaths(STARDNA);
                            result.Result = STARDNA;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> SaveDNA()
        {
            return SaveDNA(STARDNAPath, STARDNA);
        }

        public static OASISResult<bool> SaveDNA(string STARDNAPath, STARDNA STARDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");
                else if (STARDNA == null)
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.SaveDNA. Reason: STARDNA cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveSTARDNAPath(STARDNAPath);
                    string dir = Path.GetDirectoryName(effectivePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    STARDNAManager.STARDNA = STARDNA;
                    STARDNAManager.STARDNAPath = effectivePath;

                    // Persist resolved paths so the DNA file shows where STAR is actually using (factory default is blank → STAR finds best paths on load)
                    string json = JsonConvert.SerializeObject(STARDNA);
                    using (var writer = new StreamWriter(effectivePath))
                        writer.Write(json);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync()
        {
            return await SaveDNAAsync(STARDNAPath, STARDNA);
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync(string STARDNAPath, STARDNA STARDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");
                else if (STARDNA == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNA cannot be null.");
                else
                {
                    string effectivePath = ResolveEffectiveSTARDNAPath(STARDNAPath);
                    string dir = Path.GetDirectoryName(effectivePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    STARDNAManager.STARDNA = STARDNA;
                    STARDNAManager.STARDNAPath = effectivePath;

                    // Persist resolved paths so the DNA file shows where STAR is actually using (factory default is blank → STAR finds best paths on load)
                    string json = JsonConvert.SerializeObject(STARDNA);
                    using (var writer = new StreamWriter(effectivePath))
                        await writer.WriteAsync(json);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Resolves <see cref="STARDNA.STARBasePath"/> and <see cref="STARDNA.STARNETBasePath"/> after load or when constructing a new instance.
        /// Blank STARBasePath → app executable/output folder. Blank STARNETBasePath → STARBasePath/STARNET.
        /// Read-only installs redirect writable paths under the user data directory.
        /// </summary>
        public static void ResolveRuntimeBasePaths(STARDNA starDNA)
        {
            if (starDNA == null) return;

            string appRoot = AppPathHelper.ResolveAppRootDirectory();

            starDNA.STARBasePath = AppPathHelper.ResolveBasePath(appRoot, starDNA.STARBasePath);

            // If resolved base path does not contain DNATemplates (e.g. stale path from JSON), use process executable directory (e.g. /usr/local/bin)
            string csharpFolder = starDNA.CSharpDNATemplateFolder ?? "DNATemplates/CSharpDNATemplates";
            string expectedTemplates = string.IsNullOrEmpty(starDNA.STARBasePath) ? null : Path.Combine(starDNA.STARBasePath, csharpFolder);
            if (string.IsNullOrEmpty(expectedTemplates) || !Directory.Exists(expectedTemplates))
            {
                string processDir = null;
                try
                {
                    string proc = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(proc))
                        processDir = Path.GetDirectoryName(proc);
                }
                catch { /* non-fatal */ }
                if (!string.IsNullOrEmpty(processDir) && Directory.Exists(processDir))
                {
                    string altTemplates = Path.Combine(processDir, csharpFolder);
                    if (Directory.Exists(altTemplates))
                        starDNA.STARBasePath = Path.GetFullPath(processDir);
                }
            }

            if (string.IsNullOrWhiteSpace(starDNA.STARNETBasePath))
                starDNA.STARNETBasePath = Path.Combine(starDNA.STARBasePath, "STARNET");
            else
                starDNA.STARNETBasePath = AppPathHelper.ResolveBasePath(appRoot, starDNA.STARNETBasePath);

            ApplyIfSTARBasePathReadOnly(starDNA);
        }

        /// <summary>When STAR is installed to a read-only location, put STARNET and OAPPMetaDataDNA under user data.</summary>
        private static void ApplyIfSTARBasePathReadOnly(STARDNA starDNA)
        {
            if (starDNA == null) return;
            if (AppPathHelper.IsDirectoryWritable(starDNA.STARBasePath)) return;

            string userRoot = AppPathHelper.GetUserDataRoot("oasis-star-cli");
            starDNA.STARNETBasePath = Path.Combine(userRoot, "STARNET");
            starDNA.OAPPMetaDataDNAFolder = Path.Combine(userRoot, "OAPPMetaDataDNA");
        }
    }
}
