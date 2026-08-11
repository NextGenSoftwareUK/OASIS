using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {
        public NFTCommon NFTCommon { get; set; } = new NFTCommon();

        public NFTs(Guid avatarId, STARDNA STARDNA) : base(new STARNFTManager(avatarId, STARDNA),
            "Welcome to the WEB5 STAR NFT Wizard", new List<string> 
            {
                "This wizard will allow you create a WEB5 STAR NFT which wraps around a WEB4 OASIS NFT.",
                "You can mint a WEB4 OASIS NFT using the 'nft mint' sub-command.",
                "You then convert or wrap around the WEB4 OASIS NFT using the sub-command 'nft create' which will create a WEB5 STAR NFT compatible with STARNET.",
                "A WEB5 NFT can then be published to STARNET in much the same way as everything else within STAR using the same sub-commands such as publish, download, install etc.",
                "A WEB5 GeoNFT can be created from a WEB4 GeoNFT (which in turn is created from a WEB4 NFT) and can be placed in any location within Our World as part of Quest's. The main difference is WEB5 STAR NFT's can be published to STARNET, version controlled, shared, etc whereas WEB4 NFT's cannot.",
                "The wizard will create an empty folder with a NFTDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the NFT into this folder.",
                "Finally you run the sub-command 'nft publish' to convert the folder containing the NFT (can contain any number of files and sub-folders) into a OASIS NFT file (.onft) as well as optionally upload to STARNET.",
                "You can then share the .onft file with others across any platform or OS, who can then install the NFT from the file using the sub-command 'nft install'.",
                "You can also optionally choose to upload the .onft file to the STARNET store so others can search, download and install the NFT."
            },
            STAR.STARDNA.DefaultNFTsSourcePath, "DefaultNFTsSourcePath",
            STAR.STARDNA.DefaultNFTsPublishedPath, "DefaultNFTsPublishedPath",
            STAR.STARDNA.DefaultNFTsDownloadedPath, "DefaultNFTsDownloadedPath",
            STAR.STARDNA.DefaultNFTsInstalledPath, "DefaultNFTsInstalledPath")
        { }
    }
}
