using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Drawing.Text;
using System.Linq;
using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using Org.BouncyCastle.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class STARNETUIBase<T1, T2, T3, T4>
    {
        public int DisplayFieldLength { get; set; } = DEFAULT_FIELD_LENGTH;

        public STARNETUIBase(ISTARNETManagerBase<T1, T2, T3, T4> starManager, string createHeader, List<string> createIntroParagraphs, string sourcePath = "", string sourceSTARDNAKey = "", string publishedPath = "", string publishedSTARDNAKey = "", string downloadedPath = "", string downloadSTARDNAKey = "", string installedPath = "", string installedSTARDNAKey = "", int displayFieldLength = DEFAULT_FIELD_LENGTH)
        {
            starManager.OnDownloadStatusChanged += OnDownloadStatusChanged;
            starManager.OnInstallStatusChanged += OnInstallStatusChanged;
            starManager.OnPublishStatusChanged += OnPublishStatusChanged;
            starManager.OnUploadStatusChanged += OnUploadStatusChanged;

            CreateHeader = createHeader;
            CreateIntroParagraphs = createIntroParagraphs;
            IsInit = true;
            STARNETManager = starManager;
            SourcePath = sourcePath;
            SourceSTARDNAKey = sourceSTARDNAKey;
            PublishedPath = publishedPath;
            PublishedSTARDNAKey = publishedSTARDNAKey;
            DownloadedPath = downloadedPath;
            DownloadSTARDNAKey = downloadSTARDNAKey;
            InstalledPath = installedPath;
            InstalledSTARDNAKey = installedSTARDNAKey;
            DisplayFieldLength = displayFieldLength;
        }

        public virtual void Dispose()
        {
            STARNETManager.OnDownloadStatusChanged -= OnDownloadStatusChanged;
            STARNETManager.OnInstallStatusChanged -= OnInstallStatusChanged;
            STARNETManager.OnPublishStatusChanged -= OnPublishStatusChanged;
            STARNETManager.OnUploadStatusChanged -= OnUploadStatusChanged;
        }

        //public virtual async Task<OASISResult<T1>> CreateAsync(object createParams, T1 newHolon = default, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, T4 STARNETDNA = default, ProviderType providerType = ProviderType.Default)
    }
}
