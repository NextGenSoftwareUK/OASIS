using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
    }
}