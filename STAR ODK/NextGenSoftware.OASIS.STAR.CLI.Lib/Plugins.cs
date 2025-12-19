using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Plugins : STARNETUIBase<Plugin, DownloadedPlugin, InstalledPlugin, PluginDNA>
    public class Plugins : STARNETUIBase<Plugin, DownloadedPlugin, InstalledPlugin, STARNETDNA>
    {
        public Plugins(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.PluginManager(avatarId, STARDNA),
            "Welcome to the Plugin Wizard", new List<string> 
            {
                "This wizard will allow you create a Plugin, that allow you to extend STAR & STARNET.",
                "The wizard will create a plugin folder structure with generated code for both the backend manager class (extending STARNETManagerBase) and the CLI class (extending STARNETUIBase).",
                "You can customize the generated code and add your own functionality. The wizard will also help you create CLI sub-commands for your plugin.",
                "Finally you run the sub-command 'plugin publish' to convert the folder containing the plugin (can contain any number of files and sub-folders) into a OASIS Plugin file (.oplugin) as well as optionally upload to STARNET.",
                "You can then share the .oplugin file with others across any platform or OS, who can then install the Plugin from the file using the sub-command 'plugin install'.",
                "You can also optionally choose to upload the .oplugin file to the STARNET store so others can search, download and install the plugin."
            },
            STAR.STARDNA.DefaultPluginsSourcePath, "DefaultPluginsSourcePath",
            STAR.STARDNA.DefaultPluginsPublishedPath, "DefaultPluginsPublishedPath",
            STAR.STARDNA.DefaultPluginsDownloadedPath, "DefaultPluginsDownloadedPath",
            STAR.STARDNA.DefaultPluginsInstalledPath, "DefaultPluginsInstalledPath")
        { }

        public override async Task<OASISResult<Plugin>> CreateAsync(ISTARNETCreateOptions<Plugin, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Plugin> result = new OASISResult<Plugin>();
            
            if (showHeaderAndInro)
                ShowHeader();

            string pluginName = CLIEngine.GetValidInput("What is the name of the Plugin?");
            if (pluginName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            string pluginDesc = CLIEngine.GetValidInput("What is the description of the Plugin?");
            if (pluginDesc == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            // Get plugin namespace
            string pluginNamespace = CLIEngine.GetValidInput($"What namespace should be used for the plugin? (default: NextGenSoftware.OASIS.Plugins.{pluginName.Replace(" ", "")})");
            if (pluginNamespace == "exit")
            {
                result.Message = "User Exited";
                return result;
            }
            if (string.IsNullOrEmpty(pluginNamespace))
                pluginNamespace = $"NextGenSoftware.OASIS.Plugins.{pluginName.Replace(" ", "")}";

            // Get manager class name
            string managerClassName = CLIEngine.GetValidInput($"What should the Manager class be named? (default: {pluginName.Replace(" ", "")}Manager)");
            if (managerClassName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }
            if (string.IsNullOrEmpty(managerClassName))
                managerClassName = $"{pluginName.Replace(" ", "")}Manager";

            // Get CLI class name
            string cliClassName = CLIEngine.GetValidInput($"What should the CLI class be named? (default: {pluginName.Replace(" ", "")}CLI)");
            if (cliClassName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }
            if (string.IsNullOrEmpty(cliClassName))
                cliClassName = $"{pluginName.Replace(" ", "")}CLI";

            // Ask if they want to create CLI sub-commands
            bool createCLICommands = CLIEngine.GetConfirmation("Do you want to create CLI sub-commands for this plugin?");
            List<string> cliCommands = new List<string>();
            if (createCLICommands)
            {
                Console.WriteLine("Enter CLI command names (one per line, type 'done' when finished):");
                string command = "";
                do
                {
                    command = CLIEngine.GetValidInput("Command name (or 'done' to finish):");
                    if (command != "done" && command != "exit" && !string.IsNullOrEmpty(command))
                        cliCommands.Add(command);
                } while (command != "done" && command != "exit");
                
                if (command == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }
            }

            // Get folder path - use plugin name as subdirectory
            string basePluginPath = "";
            if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                basePluginPath = SourcePath;
            else
                basePluginPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, SourcePath);

            // Create plugin-specific subdirectory
            string pluginPath = Path.Combine(basePluginPath, pluginName.Replace(" ", "_"));
            
            (result, pluginPath) = GetValidFolder(result, pluginPath, STARNETManager.STARNETHolonUIName, SourceSTARDNAKey, true, "");
            if (result.IsError)
                return result;

            // Create the plugin using base class
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Generating Plugin...");
            result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, pluginName, pluginDesc, null, pluginPath, createOptions: createOptions, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                // Generate plugin code files
                await GeneratePluginCodeAsync(pluginPath, pluginName, pluginDesc, pluginNamespace, managerClassName, cliClassName, cliCommands);
                
                // Generate STARDNA.json partial file
                await GenerateSTARDNAPartialAsync(pluginPath, pluginName);
                
                CLIEngine.ShowSuccessMessage("Plugin Successfully Generated.");
                await ShowAsync(result.Result);
                Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Do you wish to open the plugin folder now?"))
                    Process.Start("explorer.exe", pluginPath);
            }

            return result;
        }

        private async Task GeneratePluginCodeAsync(string pluginPath, string pluginName, string pluginDesc, string pluginNamespace, string managerClassName, string cliClassName, List<string> cliCommands)
        {
            try
            {
                // Create Source folder structure
                string sourceFolder = Path.Combine(pluginPath, "Source");
                if (!Directory.Exists(sourceFolder))
                    Directory.CreateDirectory(sourceFolder);

                string managersFolder = Path.Combine(sourceFolder, "Managers");
                if (!Directory.Exists(managersFolder))
                    Directory.CreateDirectory(managersFolder);

                string cliFolder = Path.Combine(sourceFolder, "CLI");
                if (!Directory.Exists(cliFolder))
                    Directory.CreateDirectory(cliFolder);

                // Generate Manager class
                string managerCode = GenerateManagerClass(pluginName, pluginDesc, pluginNamespace, managerClassName, cliCommands);
                string managerFilePath = Path.Combine(managersFolder, $"{managerClassName}.cs");
                await File.WriteAllTextAsync(managerFilePath, managerCode);

                // Generate CLI class
                string cliCode = GenerateCLIClass(pluginName, pluginDesc, pluginNamespace, cliClassName, managerClassName, cliCommands);
                string cliFilePath = Path.Combine(cliFolder, $"{cliClassName}.cs");
                await File.WriteAllTextAsync(cliFilePath, cliCode);

                // Generate README
                string readmeContent = GenerateReadme(pluginName, pluginDesc, pluginNamespace, managerClassName, cliClassName, cliCommands);
                string readmePath = Path.Combine(pluginPath, "README.md");
                await File.WriteAllTextAsync(readmePath, readmeContent);

                CLIEngine.ShowSuccessMessage($"Generated plugin code files in {sourceFolder}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error generating plugin code: {ex.Message}");
            }
        }

        private async Task GenerateSTARDNAPartialAsync(string pluginPath, string pluginName)
        {
            try
            {
                string pluginFolderName = pluginName.Replace(" ", "_");
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("    // =========================================");
                sb.AppendLine($"    // {pluginName} Plugin - Custom Paths Configuration");
                sb.AppendLine("    // =========================================");
                sb.AppendLine("    // INSTRUCTIONS:");
                sb.AppendLine("    // 1. Copy the JSON below");
                sb.AppendLine("    // 2. Open your main STARDNA.json file");
                sb.AppendLine("    // 3. Find the 'Plugins' section (or create it if it doesn't exist)");
                sb.AppendLine("    // 4. Paste this JSON into the Plugins section");
                sb.AppendLine("    // 5. Save the file");
                sb.AppendLine("    //");
                sb.AppendLine("    // Note: These paths are relative to BaseSTARNETPath in your STARDNA.json");
                sb.AppendLine("    // =========================================");
                sb.AppendLine();
                sb.AppendLine($"    \"{pluginFolderName}PluginSourcePath\": \"Plugins\\\\Source\\\\{pluginFolderName}\",");
                sb.AppendLine($"    \"{pluginFolderName}PluginPublishedPath\": \"Plugins\\\\Published\\\\{pluginFolderName}\",");
                sb.AppendLine($"    \"{pluginFolderName}PluginDownloadedPath\": \"Plugins\\\\Downloaded\\\\{pluginFolderName}\",");
                sb.AppendLine($"    \"{pluginFolderName}PluginInstalledPath\": \"Plugins\\\\Installed\\\\{pluginFolderName}\"");
                sb.AppendLine("}");

                string stardnaPath = Path.Combine(pluginPath, "STARDNA_Partial.json");
                await File.WriteAllTextAsync(stardnaPath, sb.ToString());

                CLIEngine.ShowSuccessMessage($"Generated STARDNA partial configuration file: {stardnaPath}");
                CLIEngine.ShowMessage("Please follow the instructions in the file to add these paths to your main STARDNA.json");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error generating STARDNA partial: {ex.Message}");
            }
        }

        private string GenerateManagerClass(string pluginName, string pluginDesc, string pluginNamespace, string managerClassName, List<string> cliCommands)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using NextGenSoftware.OASIS.Common;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.DNA;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.Core.Enums;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.Core.Objects;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.Core.Interfaces;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Holons;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;");
            sb.AppendLine("using NextGenSoftware.OASIS.STAR.DNA;");
            sb.AppendLine();
            sb.AppendLine($"namespace {pluginNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {pluginDesc}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {managerClassName} : STARNETManagerBase<Plugin, DownloadedPlugin, InstalledPlugin, STARNETDNA>, IPluginManager");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {managerClassName}(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,");
            sb.AppendLine("            STARDNA,");
            sb.AppendLine("            OASISDNA,");
            sb.AppendLine("            typeof(Plugin),");
            sb.AppendLine("            HolonType.Plugin,");
            sb.AppendLine("            HolonType.InstalledPlugin,");
            sb.AppendLine("            \"Plugin\",");
            sb.AppendLine("            \"STARNETHolonId\",");
            sb.AppendLine("            \"PluginName\",");
            sb.AppendLine("            \"PluginType\",");
            sb.AppendLine("            \"plugin\",");
            sb.AppendLine("            \"oasis_plugins\",");
            sb.AppendLine("            \"PluginDNA.json\",");
            sb.AppendLine("            \"PluginDNAJSON\")");
            sb.AppendLine("        { }");
            sb.AppendLine();
            sb.AppendLine($"        public {managerClassName}(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,");
            sb.AppendLine("            STARDNA,");
            sb.AppendLine("            OASISDNA,");
            sb.AppendLine("            typeof(Plugin),");
            sb.AppendLine("            HolonType.Plugin,");
            sb.AppendLine("            HolonType.InstalledPlugin,");
            sb.AppendLine("            \"Plugin\",");
            sb.AppendLine("            \"STARNETHolonId\",");
            sb.AppendLine("            \"PluginName\",");
            sb.AppendLine("            \"PluginType\",");
            sb.AppendLine("            \"plugin\",");
            sb.AppendLine("            \"oasis_plugins\",");
            sb.AppendLine("            \"PluginDNA.json\",");
            sb.AppendLine("            \"PluginDNAJSON\")");
            sb.AppendLine("        { }");
            sb.AppendLine();
            
            // Generate method stubs for each custom command
            if (cliCommands.Count > 0)
            {
                sb.AppendLine("        // Custom command methods - implement your plugin logic here");
                foreach (var command in cliCommands)
                {
                    string methodName = command.Replace(" ", "").Replace("-", "");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Executes the '{command}' command");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public async Task<OASISResult<bool>> Execute{methodName}Async(Guid avatarId, string[] args = null, ProviderType providerType = ProviderType.Default)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            OASISResult<bool> result = new OASISResult<bool>();");
                    sb.AppendLine("            // TODO: Implement your '{command}' command logic here");
                    sb.AppendLine("            // Example:");
                    sb.AppendLine("            // result.Result = true;");
                    sb.AppendLine("            // result.Message = \"Command executed successfully\";");
                    sb.AppendLine("            return result;");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateCLIClass(string pluginName, string pluginDesc, string pluginNamespace, string cliClassName, string managerClassName, List<string> cliCommands)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using NextGenSoftware.CLI.Engine;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.Core.Enums;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.Core.Objects;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Holons;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Objects;");
            sb.AppendLine("using NextGenSoftware.OASIS.Common;");
            sb.AppendLine("using NextGenSoftware.OASIS.STAR.DNA;");
            sb.AppendLine("using System.Drawing;");
            sb.AppendLine();
            sb.AppendLine($"namespace {pluginNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// CLI interface for {pluginName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {cliClassName} : STARNETUIBase<Plugin, DownloadedPlugin, InstalledPlugin, STARNETDNA>");
            sb.AppendLine("    {");
            sb.AppendLine($"        private {managerClassName} _manager;");
            sb.AppendLine();
            sb.AppendLine($"        public {cliClassName}(Guid avatarId, STARDNA STARDNA) : base(new {managerClassName}(avatarId, STARDNA),");
            sb.AppendLine($"            \"Welcome to the {pluginName} Plugin\", new List<string>");
            sb.AppendLine("            {");
            sb.AppendLine($"                \"{pluginDesc}\"");
            sb.AppendLine("            },");
            sb.AppendLine("            STAR.STARDNA.DefaultPluginsSourcePath, \"DefaultPluginsSourcePath\",");
            sb.AppendLine("            STAR.STARDNA.DefaultPluginsPublishedPath, \"DefaultPluginsPublishedPath\",");
            sb.AppendLine("            STAR.STARDNA.DefaultPluginsDownloadedPath, \"DefaultPluginsDownloadedPath\",");
            sb.AppendLine("            STAR.STARDNA.DefaultPluginsInstalledPath, \"DefaultPluginsInstalledPath\")");
            sb.AppendLine("        {");
            sb.AppendLine($"            _manager = ({managerClassName})STARNETManager;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Generate RunPlugin method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Main entry point for running the plugin - displays custom menu and handles commands");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public async Task<OASISResult<bool>> RunPluginAsync(ProviderType providerType = ProviderType.Default)");
            sb.AppendLine("        {");
            sb.AppendLine("            OASISResult<bool> result = new OASISResult<bool>();");
            sb.AppendLine("            bool exit = false;");
            sb.AppendLine();
            sb.AppendLine("            CLIEngine.ShowMessage(\"\", false);");
            sb.AppendLine($"            CLIEngine.WriteAsciMessage(\" {pluginName.ToUpper()} PLUGIN\", System.Drawing.Color.Green);");
            sb.AppendLine("            CLIEngine.ShowMessage(\"\", false);");
            sb.AppendLine();
            sb.AppendLine("            do");
            sb.AppendLine("            {");
            sb.AppendLine("                Console.ForegroundColor = ConsoleColor.Yellow;");
            sb.AppendLine($"                CLIEngine.ShowMessage($\"{pluginName}: \", false, true);");
            sb.AppendLine("                string input = Console.ReadLine();");
            sb.AppendLine();
            sb.AppendLine("                if (!string.IsNullOrEmpty(input))");
            sb.AppendLine("                {");
            sb.AppendLine("                    string[] inputArgs = input.Split(\" \");");
            sb.AppendLine();
            sb.AppendLine("                    if (inputArgs.Length > 0)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        switch (inputArgs[0].ToLower())");
            sb.AppendLine("                        {");
            
            // Add cases for each command
            foreach (var command in cliCommands)
            {
                string methodName = command.Replace(" ", "").Replace("-", "");
                sb.AppendLine($"                            case \"{command.ToLower()}\":");
                sb.AppendLine($"                            case \"{command.Replace(" ", "").ToLower()}\":");
                sb.AppendLine($"                                await {methodName}Async(inputArgs, providerType);");
                sb.AppendLine("                                break;");
            }
            
            sb.AppendLine("                            case \"exit\":");
            sb.AppendLine("                            case \"quit\":");
            sb.AppendLine("                                exit = true;");
            sb.AppendLine("                                break;");
            sb.AppendLine("                            case \"help\":");
            sb.AppendLine("                                ShowHelp();");
            sb.AppendLine("                                break;");
            sb.AppendLine("                            default:");
            sb.AppendLine($"                                CLIEngine.ShowErrorMessage($\"Unknown command: {{inputArgs[0]}}. Type 'help' for available commands.\");");
            sb.AppendLine("                                break;");
            sb.AppendLine("                        }");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            } while (!exit);");
            sb.AppendLine();
            sb.AppendLine("            result.Result = true;");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Generate ShowHelp method
            sb.AppendLine("        private void ShowHelp()");
            sb.AppendLine("        {");
            sb.AppendLine("            CLIEngine.ShowMessage(\"\", false);");
            sb.AppendLine($"            CLIEngine.ShowMessage(\"Available commands for {pluginName}:\");");
            foreach (var command in cliCommands)
            {
                sb.AppendLine($"            CLIEngine.ShowMessage(\"  {command} - Execute {command} command\");");
            }
            sb.AppendLine("            CLIEngine.ShowMessage(\"  help - Show this help message\");");
            sb.AppendLine("            CLIEngine.ShowMessage(\"  exit/quit - Exit the plugin\");");
            sb.AppendLine("            CLIEngine.ShowMessage(\"\", false);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Generate command methods
            if (cliCommands.Count > 0)
            {
                sb.AppendLine("        // CLI Command Methods - implement your command logic here");
                foreach (var command in cliCommands)
                {
                    string methodName = command.Replace(" ", "").Replace("-", "");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Executes the '{command}' command");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public async Task<OASISResult<bool>> {methodName}Async(string[] args = null, ProviderType providerType = ProviderType.Default)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            OASISResult<bool> result = new OASISResult<bool>();");
                    sb.AppendLine($"            CLIEngine.ShowMessage(\"Executing {command} command...\");");
                    sb.AppendLine();
                    sb.AppendLine("            // TODO: Implement your command logic here");
                    sb.AppendLine("            // Example:");
                    sb.AppendLine($"            // OASISResult<bool> managerResult = await _manager.Execute{methodName}Async(STAR.BeamedInAvatar.Id, args, providerType);");
                    sb.AppendLine("            // if (managerResult != null && !managerResult.IsError)");
                    sb.AppendLine("            // {");
                    sb.AppendLine("            //     result.Result = managerResult.Result;");
                    sb.AppendLine("            //     CLIEngine.ShowSuccessMessage($\"{command} executed successfully\");");
                    sb.AppendLine("            // }");
                    sb.AppendLine("            // else");
                    sb.AppendLine("            // {");
                    sb.AppendLine("            //     CLIEngine.ShowErrorMessage($\"Error executing {command}: {{managerResult.Message}}\");");
                    sb.AppendLine("            // }");
                    sb.AppendLine();
                    sb.AppendLine("            return result;");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateReadme(string pluginName, string pluginDesc, string pluginNamespace, string managerClassName, string cliClassName, List<string> cliCommands)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {pluginName} Plugin");
            sb.AppendLine();
            sb.AppendLine(pluginDesc);
            sb.AppendLine();
            sb.AppendLine("## Structure");
            sb.AppendLine();
            sb.AppendLine("- `Source/Managers/{managerClassName}.cs` - Backend manager class extending STARNETManagerBase");
            sb.AppendLine($"- `Source/CLI/{cliClassName}.cs` - CLI interface class extending STARNETUIBase");
            sb.AppendLine();
            if (cliCommands.Count > 0)
            {
                sb.AppendLine("## CLI Commands");
                sb.AppendLine();
                foreach (var command in cliCommands)
                {
                    sb.AppendLine($"- `{command}` - Custom command for this plugin");
                }
                sb.AppendLine();
            }
            sb.AppendLine("## Usage");
            sb.AppendLine();
            sb.AppendLine("1. Customize the generated code in the Source folder");
            sb.AppendLine("2. Add your plugin logic to the Manager class");
            sb.AppendLine("3. Implement CLI commands in the CLI class");
            sb.AppendLine("4. Run `plugin publish` to create the .oplugin file");
            sb.AppendLine("5. Share or upload to STARNET for others to use");
            return sb.ToString();
        }

        public async Task<OASISResult<IEnumerable<InstalledPlugin>>> ListInstalledAsync(ProviderType providerType = ProviderType.Default)
        {
            return await ListAllInstalledForBeamedInAvatarAsync(providerType);
        }

        public async Task<OASISResult<IEnumerable<InstalledPlugin>>> ListUninstalledAsync(ProviderType providerType = ProviderType.Default)
        {
            return await ListAllUninstalledForBeamedInAvatarAsync(providerType);
        }

        public async Task DisableAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            await DeactivateAsync(idOrName, providerType);
        }

        public async Task EnableAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            await ActivateAsync(idOrName, providerType);
        }

        public override async Task<OASISResult<IEnumerable<InstalledPlugin>>> ListAllInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<InstalledPlugin>> result = await base.ListAllInstalledForBeamedInAvatarAsync(providerType);

            if (result != null && !result.IsError && result.Result != null && result.Result.Any())
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to run one of the installed plugins?"))
                {
                    int number = 0;
                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("Enter the number of the plugin you wish to run:");
                        if (number < 1 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    } while (number < 1 || number > result.Result.Count());

                    if (number > 0)
                    {
                        InstalledPlugin plugin = result.Result.ElementAt(number - 1);
                        if (plugin != null)
                        {
                            await RunInstalledPluginAsync(plugin, providerType);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<OASISResult<bool>> RunInstalledPluginAsync(InstalledPlugin plugin, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            
            try
            {
                CLIEngine.ShowWorkingMessage($"Loading plugin '{plugin.Name}'...");
                
                // Use PluginLoader to dynamically load and run the plugin
                var loader = new PluginLoader();
                var loadResult = await loader.LoadAndRunPluginAsync(plugin, providerType);
                
                if (loadResult != null && !loadResult.IsError)
                {
                    result.Result = loadResult.Result;
                    result.Message = loadResult.Message;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error running plugin: {loadResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error running plugin: {ex.Message}", ex);
            }

            return result;
        }
    }
}