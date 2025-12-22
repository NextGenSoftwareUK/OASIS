using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// Dynamically loads and runs STAR CLI plugins at runtime
    /// Uses AssemblyLoadContext for .NET Core/5+ plugin loading
    /// </summary>
    public class PluginLoader
    {
        private static Dictionary<string, AssemblyLoadContext> _loadedContexts = new Dictionary<string, AssemblyLoadContext>();
        private static Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

        /// <summary>
        /// Scans the plugins installed folder and loads all available plugins
        /// </summary>
        public async Task<OASISResult<List<InstalledPlugin>>> ScanAndLoadPluginsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<InstalledPlugin>> result = new OASISResult<List<InstalledPlugin>>();
            result.Result = new List<InstalledPlugin>();

            try
            {
                string installedPath = "";
                if (Path.IsPathRooted(STAR.STARDNA.DefaultPluginsInstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                    installedPath = STAR.STARDNA.DefaultPluginsInstalledPath;
                else
                    installedPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultPluginsInstalledPath);

                if (!Directory.Exists(installedPath))
                {
                    result.Message = $"Plugins installed folder does not exist: {installedPath}";
                    return result;
                }

                // Scan for plugin folders
                var pluginFolders = Directory.GetDirectories(installedPath);
                
                foreach (var pluginFolder in pluginFolders)
                {
                    try
                    {
                        // Look for PluginDNA.json file
                        string dnaPath = Path.Combine(pluginFolder, "PluginDNA.json");
                        if (File.Exists(dnaPath))
                        {
                            // Try to load the plugin assembly if it exists
                            var pluginInfo = await LoadPluginInfoAsync(pluginFolder);
                            if (pluginInfo != null)
                            {
                                result.Result.Add(pluginInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CLIEngine.ShowErrorMessage($"Error scanning plugin folder {pluginFolder}: {ex.Message}");
                    }
                }

                result.IsSaved = true;
                result.Message = $"Found {result.Result.Count} installed plugin(s)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error scanning plugins: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Loads plugin information from a plugin folder
        /// </summary>
        private async Task<InstalledPlugin> LoadPluginInfoAsync(string pluginFolder)
        {
            try
            {
                string dnaPath = Path.Combine(pluginFolder, "PluginDNA.json");
                if (!File.Exists(dnaPath))
                    return null;

                // Read and deserialize PluginDNA.json to get plugin metadata
                string dnaContent = await File.ReadAllTextAsync(dnaPath);
                
                try
                {
                    // Try to deserialize as STARNETDNA
                    var starNetDNA = JsonConvert.DeserializeObject<STARNETDNA>(dnaContent);
                    
                    if (starNetDNA != null)
                    {
                        var plugin = new InstalledPlugin
                        {
                            Name = starNetDNA.Name ?? Path.GetFileName(pluginFolder),
                            Description = starNetDNA.Description,
                            Version = starNetDNA.Version,
                            Id = starNetDNA.Id != Guid.Empty ? starNetDNA.Id : Guid.NewGuid(),
                            STARNETDNA = starNetDNA
                        };

                        return plugin;
                    }
                }
                catch (JsonException)
                {
                    // If deserialization fails, create basic plugin from folder name
                    CLIEngine.ShowWarningMessage($"Could not fully parse PluginDNA.json for {Path.GetFileName(pluginFolder)}, using basic info");
                }
                
                // Fallback: create basic InstalledPlugin from folder name
                var basicPlugin = new InstalledPlugin
                {
                    Name = Path.GetFileName(pluginFolder),
                    Id = Guid.NewGuid()
                };

                return basicPlugin;
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error loading plugin info from {pluginFolder}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Dynamically loads and runs a plugin
        /// </summary>
        public async Task<OASISResult<bool>> LoadAndRunPluginAsync(InstalledPlugin plugin, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Find plugin folder
                string installedPath = "";
                if (Path.IsPathRooted(STAR.STARDNA.DefaultPluginsInstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                    installedPath = STAR.STARDNA.DefaultPluginsInstalledPath;
                else
                    installedPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultPluginsInstalledPath);

                string pluginFolder = Path.Combine(installedPath, plugin.Name.Replace(" ", "_"));
                
                if (!Directory.Exists(pluginFolder))
                {
                    OASISErrorHandling.HandleError(ref result, $"Plugin folder not found: {pluginFolder}");
                    return result;
                }

                // Look for compiled DLL - check both Source folder and root plugin folder
                string sourceFolder = Path.Combine(pluginFolder, "Source");
                List<string> searchPaths = new List<string>();
                
                if (Directory.Exists(sourceFolder))
                    searchPaths.Add(sourceFolder);
                searchPaths.Add(pluginFolder);
                
                // Try to load using AssemblyLoadContext
                var context = new AssemblyLoadContext($"Plugin_{plugin.Name}", true);
                
                // Look for DLL files in all search paths
                List<string> dllFiles = new List<string>();
                foreach (var searchPath in searchPaths)
                {
                    var foundDlls = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                    dllFiles.AddRange(foundDlls);
                }
                
                if (dllFiles.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No compiled DLL found for plugin '{plugin.Name}'. Please compile the plugin first. Expected location: {pluginFolder} or {sourceFolder}");
                    return result;
                }

                // Load the main plugin assembly - try to find the one containing the CLI class
                Assembly pluginAssembly = null;
                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        var assembly = context.LoadFromAssemblyPath(dllFile);
                        
                        // Check if this assembly contains a class extending STARNETUIBase
                        var hasCLIClass = assembly.GetTypes()
                            .Any(t => t.IsClass && 
                                !t.IsAbstract && 
                                t.BaseType != null && 
                                t.BaseType.Name.Contains("STARNETUIBase"));
                        
                        if (hasCLIClass)
                        {
                            pluginAssembly = assembly;
                            break; // Found the main plugin assembly
                        }
                        
                        // If no main assembly found yet, use the first one
                        if (pluginAssembly == null)
                            pluginAssembly = assembly;
                    }
                    catch (Exception ex)
                    {
                        CLIEngine.ShowErrorMessage($"Error loading DLL {dllFile}: {ex.Message}");
                    }
                }

                if (pluginAssembly == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not load plugin assembly");
                    return result;
                }

                // Find the CLI class that extends STARNETUIBase
                var cliType = pluginAssembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && 
                        !t.IsAbstract && 
                        t.BaseType != null && 
                        t.BaseType.Name.Contains("STARNETUIBase"));

                if (cliType == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find CLI class extending STARNETUIBase in plugin");
                    return result;
                }

                // Instantiate the CLI class
                var cliInstance = Activator.CreateInstance(cliType, STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                
                if (cliInstance == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not instantiate plugin CLI class");
                    return result;
                }

                // Find and call RunPluginAsync method
                var runMethod = cliType.GetMethod("RunPluginAsync", BindingFlags.Public | BindingFlags.Instance);
                
                if (runMethod == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find RunPluginAsync method in plugin");
                    return result;
                }

                // Invoke RunPluginAsync
                var runResult = runMethod.Invoke(cliInstance, new object[] { providerType });
                
                if (runResult is Task<OASISResult<bool>> task)
                {
                    var taskResult = await task;
                    result = taskResult;
                }
                else if (runResult is OASISResult<bool> directResult)
                {
                    result = directResult;
                }
                else
                {
                    result.Result = true;
                    result.Message = "Plugin executed successfully";
                }

                // Store context for potential unloading later
                _loadedContexts[plugin.Name] = context;
                _loadedAssemblies[plugin.Name] = pluginAssembly;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading and running plugin: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Unloads a plugin from memory
        /// </summary>
        public void UnloadPlugin(string pluginName)
        {
            if (_loadedContexts.ContainsKey(pluginName))
            {
                var context = _loadedContexts[pluginName];
                context.Unload();
                _loadedContexts.Remove(pluginName);
                _loadedAssemblies.Remove(pluginName);
            }
        }

        /// <summary>
        /// Unloads all loaded plugins
        /// </summary>
        public void UnloadAllPlugins()
        {
            foreach (var context in _loadedContexts.Values)
            {
                context.Unload();
            }
            _loadedContexts.Clear();
            _loadedAssemblies.Clear();
        }
    }
}






