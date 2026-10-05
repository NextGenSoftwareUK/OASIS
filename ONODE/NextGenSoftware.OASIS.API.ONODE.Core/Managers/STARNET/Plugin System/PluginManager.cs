using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class PluginManager : STARNETManagerBase<Plugin, DownloadedPlugin, InstalledPlugin, PluginDNA>, IPluginManager
    public class PluginManager : STARNETManagerBase<Plugin, DownloadedPlugin, InstalledPlugin, STARNETDNA>, IPluginManager
    {
        public PluginManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(Plugin),
            HolonType.Plugin,
            HolonType.InstalledPlugin,
            "Plugin",
            //"PluginId",
            "STARNETHolonId",
            "PluginName",
            "PluginType",
            "plugin",
            "oasis_plugins",
            "PluginDNA.json",
            "PluginDNAJSON")
        { }

        public PluginManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(Plugin),
            HolonType.Plugin,
            HolonType.InstalledPlugin,
            "Plugin",
            //"PluginId",
            "STARNETHolonId",
            "PluginName",
            "PluginType",
            "plugin",
            "oasis_plugins",
            "PluginDNA.json",
            "PluginDNAJSON")
        { }

        public async Task<OASISResult<bool>> UninstallAsync(Guid avatarId, Guid pluginId)
        {
            try
            {
                // Load the installed plugin
                var pluginResult = await LoadInstalledAsync(avatarId, pluginId, 0);
                
                if (pluginResult.IsError || pluginResult.Result == null)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Plugin with ID {pluginId} not found or not installed."
                    };
                }

                var installedPlugin = pluginResult.Result;
                
                // Check if plugin is currently active
                if (installedPlugin.IsActive)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Cannot uninstall plugin '{installedPlugin.Name}' because it is currently active. Please stop the plugin first."
                    };
                }

                // Delete the installed plugin
                var deleteResult = await DeleteAsync(avatarId, pluginId, 0);
                
                if (deleteResult.IsError)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Error uninstalling plugin: {deleteResult.Message}"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = true,
                    Message = $"Successfully uninstalled plugin '{installedPlugin.Name}'."
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error uninstalling plugin: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}