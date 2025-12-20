using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
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

                // Read PluginDNA.json to get plugin metadata
                string dnaContent = await File.ReadAllTextAsync(dnaPath);
                // TODO: Deserialize to get plugin info
                // For now, create a basic InstalledPlugin from folder name
                
                var plugin = new InstalledPlugin
                {
                    Name = Path.GetFileName(pluginFolder),
                    // Add more properties from DNA file
                };

                return plugin;
            }
            catch
            {
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

                // Look for compiled DLL in Source folder
                string sourceFolder = Path.Combine(pluginFolder, "Source");
                if (!Directory.Exists(sourceFolder))
                {
                    OASISErrorHandling.HandleError(ref result, $"Plugin source folder not found: {sourceFolder}");
                    return result;
                }

                // Find the CLI class DLL (assuming it's compiled to a DLL)
                // For now, we'll use reflection to find and instantiate the CLI class
                // In a real scenario, the plugin would need to be compiled first
                
                // Try to load using AssemblyLoadContext
                var context = new AssemblyLoadContext($"Plugin_{plugin.Name}", true);
                
                // Look for DLL files
                var dllFiles = Directory.GetFiles(sourceFolder, "*.dll", SearchOption.AllDirectories);
                
                if (dllFiles.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No compiled DLL found for plugin. Please compile the plugin first.");
                    return result;
                }

                // Load the main plugin assembly
                Assembly pluginAssembly = null;
                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        pluginAssembly = context.LoadFromAssemblyPath(dllFile);
                        break; // Load first DLL found (in production, you'd want to find the main one)
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



