using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class ZomesMetaDataDNA : STARNETUIBase<ZomeMetaDataDNA, DownloadedZomeMetaDataDNA, InstalledZomeMetaDataDNA, STARNETDNA>
    {
        public ZomesMetaDataDNA(Guid avatarId) : base(new API.ONODE.Core.Managers.ZomeMetaDataDNAManager(avatarId),
            "Welcome to the Zome MetaData DNA Wizard", new List<string> 
            {
                "This wizard will allow you create Zome MetaData DNA which contain MetaData for Zomes's & Holon's.",
                "Zome MetaData DNA contains Zome MetaData DNA which in turn contains Holon MetaData DNA.",
                "The wizard will create an empty folder with a ZomeMetaDataDNA.json file in it. You then simply place the Zome MetaData DNA and any other files/folders you need for the assets (optional) for the Zome MetaData DNA into this folder.",
                "Finally you run the sub-command 'zome metadata publish' to convert the folder containing the Zome MetaData DNA (can contain any number of files and sub-folders) into a OASIS Zome MetaData DNA file (.ozomemetadata) as well as optionally upload to STARNET.",
                "You can then share the .ozomemetadata file with others across any platform or OS, who can then install the Zome MetaData DNA from the file using the sub-command 'zome metadata install'.",
                "You can also optionally choose to upload the .ozomemetadata file to the STARNET store so others can search, download and install the Zome MetaData DNA."
            },
            STAR.STARDNA.DefaultZomesMetaDataDNASourcePath, "DefaultZomesMetaDataDNASourcePath",
            STAR.STARDNA.DefaultZomesMetaDataDNAPublishedPath, "DefaultZomesMetaDataDNAPublishedPath",
            STAR.STARDNA.DefaultZomesMetaDataDNADownloadedPath, "DefaultZomesMetaDataDNADownloadedPath",
            STAR.STARDNA.DefaultZomesMetaDataDNAInstalledPath, "DefaultZomesMetaDataDNAInstalledPath",
            50)
        { }
    }
}