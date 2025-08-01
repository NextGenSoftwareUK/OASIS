﻿using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class PluginManager : STARNETManagerBase<Plugin, DownloadedPlugin, InstalledPlugin, PluginDNA>, IPluginManager
    {
        public PluginManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(Plugin),
            HolonType.Plugin,
            HolonType.InstalledPlugin,
            "Plugin",
            "PluginId",
            "PluginName",
            "PluginType",
            "plugin",
            "oasis_plugins",
            "PluginDNA.json",
            "PluginDNAJSON")
        { }

        public PluginManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(Plugin),
            HolonType.Plugin,
            HolonType.InstalledPlugin,
            "Plugin",
            "PluginId",
            "PluginName",
            "PluginType",
            "plugin",
            "oasis_plugins",
            "PluginDNA.json",
            "PluginDNAJSON")
        { }
    }
}