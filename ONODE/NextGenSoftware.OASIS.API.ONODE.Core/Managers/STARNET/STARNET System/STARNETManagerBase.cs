using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4> : COSMICManagerBase, ISTARNETManagerBase<T1, T2, T3, T4> where T1 : ISTARNETHolon, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        private int _progress = 0;
        private long _fileLength = 0;

        public STARNETManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public STARNETManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }
        public STARNETManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARNETCategory = null, HolonType STARNETHolonType = HolonType.STARNETHolon, HolonType STARNETHolonInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARNETHolonUIName = "OAPP System Holon", string STARNETHolonIdName = "STARNETHolonId", string STARNETHolonNameName = "STARNETHolonName", string STARNETHolonTypeName = "STARNETHolonType", string STARNETHolonFileExtention = "oappsystemholon", string STARNETHolonGoogleBucket = "oasis_oappsystemholons", string STARNETDNAFileName = "STARNETDNA.json", string STARNETDNAJSONName = "STARNETDNAJSON") : base(avatarId, OASISDNA)
        {
            this.STARDNA = STARDNA;
            this.STARNETHolonType = STARNETHolonType;
            this.STARNETHolonInstalledHolonType = STARNETHolonInstalledHolonType;
            this.STARNETHolonUIName = STARNETHolonUIName;
            this.STARNETHolonIdName = STARNETHolonIdName;
            this.STARNETHolonNameName = STARNETHolonNameName;
            this.STARNETHolonTypeName = STARNETHolonTypeName;
            this.STARNETHolonFileExtention = STARNETHolonFileExtention;
            this.STARNETHolonGoogleBucket = STARNETHolonGoogleBucket;
            this.STARNETDNAFileName = STARNETDNAFileName;
            this.STARNETDNAJSONName = STARNETDNAJSONName;
            this.STARNETCategory = STARNETCategory;
        }

        public STARNETManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARNETCategory = null, HolonType STARNETHolonType = HolonType.STARNETHolon, HolonType STARNETHolonInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARNETHolonUIName = "OAPP System Holon", string STARNETHolonIdName = "STARNETHolonId", string STARNETHolonNameName = "STARNETHolonName", string STARNETHolonTypeName = "STARNETHolonType", string STARNETHolonFileExtention = "oappsystemholon", string STARNETHolonGoogleBucket = "oasis_oappsystemholons", string STARNETDNAFileName = "STARNETDNA.json", string STARNETDNAJSONName = "STARNETDNAJSON") : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            this.STARDNA = STARDNA;
            this.STARNETHolonType = STARNETHolonType;
            this.STARNETHolonInstalledHolonType = STARNETHolonInstalledHolonType;
            this.STARNETHolonUIName = STARNETHolonUIName;
            this.STARNETHolonIdName = STARNETHolonIdName;
            this.STARNETHolonNameName = STARNETHolonNameName;
            this.STARNETHolonTypeName = STARNETHolonTypeName;
            this.STARNETHolonFileExtention = STARNETHolonFileExtention;
            this.STARNETHolonGoogleBucket = STARNETHolonGoogleBucket;
            this.STARNETDNAFileName = STARNETDNAFileName;
            this.STARNETDNAJSONName = STARNETDNAJSONName;
            this.STARNETCategory = STARNETCategory;
        }

        public delegate void PublishStatusChanged(object sender, STARNETHolonPublishStatusEventArgs e);
        public delegate void InstallStatusChanged(object sender, STARNETHolonInstallStatusEventArgs e);
        public delegate void UploadStatusChanged(object sender, STARNETHolonUploadProgressEventArgs e);
        public delegate void DownloadStatusChanged(object sender, STARNETHolonDownloadProgressEventArgs e);

        /// <summary>
        /// Fired when there is a change in the publish status.
        /// </summary>
        public event PublishStatusChanged OnPublishStatusChanged;

        /// <summary>
        /// Fired when there is a change to the Install status.
        /// </summary>
        public event InstallStatusChanged OnInstallStatusChanged;

        /// <summary>
        /// Fired when there is a change in the upload status.
        /// </summary>
        public event UploadStatusChanged OnUploadStatusChanged;

        /// <summary>
        /// Fired when there is a change in the download status.
        /// </summary>
        public event DownloadStatusChanged OnDownloadStatusChanged;

        //public bool IsInstallable { get; set; } = true; //TODO: Make this configurable in the DNA?
        public HolonType STARNETHolonType { get; set; } = HolonType.STARNETHolon;
        public HolonType STARNETHolonInstalledHolonType { get; set; } = HolonType.InstalledSTARNETHolon;
        public string STARNETHolonUIName { get; set; } = "OAPP System Holon";
        public string STARNETHolonIdName { get; set; } = "STARNETHolonId";
        public string STARNETHolonNameName { get; set; } = "STARNETHolonName";
        public string STARNETHolonTypeName { get; set; } = "STARNETHolonType";
        public string STARNETHolonFileExtention { get; set; } = "oappsystemholon";
        public string STARNETHolonGoogleBucket { get; set; } = "oasis_oappsystemholons";
        public string STARNETDNAFileName { get; set; } = "STARNETDNA.json";
        public string STARNETDNAJSONName { get; set; } = "STARNETDNAJSON";
        public Type STARNETCategory { get; set; }
        public STARDNA STARDNA { get; set; }

        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, STARNETCreateOptions<T1, T4> createOptions = null)
        //{
        //    //return CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions != null ? createOptions.ProviderType : ProviderType.Default, createOptions != null ? createOptions.MetaHolonTagMappings : null, createOptions != null ? createOptions.MetaTagMappings : null, createOptions != null ? createOptions.NewHolon : null, createOptions != null ? createOptions.STARNETDNA : null, createOptions != null ? createOptions.CheckIfSourcePathExists : true);
        //}

        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Dictionary<string, object> metaData = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //public virtual async Task<OASISResult<T1>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, T1 newHolon = default, T4 STARNETDNA = default, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
    }
}