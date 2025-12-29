using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public static class STARDNAManager
    {
        public static string STARDNAPath = Path.Combine("DNA", "STARDNA.json");
        public static STARDNA STARDNA { get; set; }

        private static string ResolveSTARDNAPath(string originalPath)
        {
            // Normalize path separators for cross-platform compatibility
            string normalizedPath = originalPath?.Replace('\\', Path.DirectorySeparatorChar) ?? Path.Combine("DNA", "STAR_DNA.json");
            
            // If the path doesn't exist, try to find STAR_DNA.json or STARDNA.json in common locations
            if (!File.Exists(normalizedPath))
            {
                // Get potential base directories to search from
                List<string> baseDirs = new List<string>
                {
                    Directory.GetCurrentDirectory(), // Current working directory
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", // Assembly location
                };
                
                // Try to find codebase root by looking for common directories
                foreach (string baseDir in baseDirs.ToArray())
                {
                    string current = baseDir;
                    for (int i = 0; i < 5 && !string.IsNullOrEmpty(current); i++)
                    {
                        if (Directory.Exists(Path.Combine(current, "OASIS Architecture")) || 
                            Directory.Exists(Path.Combine(current, "STAR ODK")))
                        {
                            baseDirs.Add(current);
                            break;
                        }
                        current = Path.GetDirectoryName(current);
                    }
                }
                
                // Try common locations relative to each base directory
                // Check both STAR_DNA.json (with underscore) and STARDNA.json (without underscore)
                string[] relativePaths = new string[]
                {
                    Path.Combine("STAR ODK", "NextGenSoftware.OASIS.STAR.STARDNA", "DNA", "STAR_DNA.json"),
                    Path.Combine("STAR ODK", "NextGenSoftware.OASIS.STAR.STARDNA", "DNA", "STARDNA.json"),
                    Path.Combine("publish", "DNA", "STAR_DNA.json"),
                    Path.Combine("DNA", "STAR_DNA.json"),
                    Path.Combine("DNA", "STARDNA.json"),
                    normalizedPath // Original path
                };
                
                foreach (string baseDir in baseDirs)
                {
                    if (string.IsNullOrEmpty(baseDir)) continue;
                    
                    foreach (string relPath in relativePaths)
                    {
                        string fullPath = Path.Combine(baseDir, relPath);
                        if (File.Exists(fullPath))
                        {
                            return fullPath;
                        }
                    }
                }
            }
            
            return normalizedPath;
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

                // Resolve the path to find the file in common locations
                string resolvedPath = ResolveSTARDNAPath(STARDNAPath);
                
                if (!File.Exists(resolvedPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");
                
                else
                {
                    STARDNAManager.STARDNAPath = resolvedPath;

                    using (StreamReader r = new StreamReader(resolvedPath))
                    {
                        string json = r.ReadToEnd();
                        STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                        result.Result = STARDNA;
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

                // Resolve the path to find the file in common locations
                string resolvedPath = ResolveSTARDNAPath(STARDNAPath);
                
                if (!File.Exists(resolvedPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");

                else
                {
                    STARDNAManager.STARDNAPath = resolvedPath;

                    using (StreamReader r = new StreamReader(resolvedPath))
                    {
                        string json = await r.ReadToEndAsync();
                        STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                        result.Result = STARDNA;
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

                // Resolve the path to find the file in common locations, or use the provided path if it doesn't exist (for creating new files)
                string resolvedPath = ResolveSTARDNAPath(STARDNAPath);
                
                // If the resolved path doesn't exist, use the original path (might be creating a new file)
                if (!File.Exists(resolvedPath))
                    resolvedPath = STARDNAPath;

                else
                {
                    if (STARDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.SaveDNA. Reason: STARDNA cannot be null.");

                    STARDNAManager.STARDNA = STARDNA;
                    STARDNAManager.STARDNAPath = resolvedPath;

                    if (!Directory.Exists(Path.GetDirectoryName(resolvedPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath));

                    string json = JsonConvert.SerializeObject(STARDNA);
                    StreamWriter writer = new StreamWriter(resolvedPath);
                    writer.Write(json);
                    writer.Close();
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

                // Resolve the path to find the file in common locations, or use the provided path if it doesn't exist (for creating new files)
                string resolvedPath = ResolveSTARDNAPath(STARDNAPath);
                
                // If the resolved path doesn't exist, use the original path (might be creating a new file)
                if (!File.Exists(resolvedPath))
                    resolvedPath = STARDNAPath;

                if (STARDNA == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNA cannot be null.");

                STARDNAManager.STARDNA = STARDNA;
                STARDNAManager.STARDNAPath = resolvedPath;

                if (!Directory.Exists(Path.GetDirectoryName(resolvedPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath));

                string json = JsonConvert.SerializeObject(STARDNA);
                StreamWriter writer = new StreamWriter(resolvedPath);
                await writer.WriteAsync(json);
                writer.Close();
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }
    }
}