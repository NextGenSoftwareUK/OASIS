using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class HolonsMetaDataDNA : STARNETUIBase<HolonMetaDataDNA, DownloadedHolonMetaDataDNA, InstalledHolonMetaDataDNA, STARNETDNA>
    {
        public HolonsMetaDataDNA(Guid avatarId) : base(new API.ONODE.Core.Managers.HolonMetaDataDNAManager(avatarId),
            "Welcome to the Holons MetaData DNA Wizard", new List<string> 
            {
                "This wizard will allow you create Holon MetaData DNA.",
                "Holon MetaData DNA contains the fields/properties that make up the Holon Data Object.",
                "The wizard will create an empty folder with a HolonMetaDataDNA.json file in it. You then simply place the Holon MetaData DNA and any other files/folders you need for the assets (optional) for the Holon MetaData DNA into this folder.",
                "Finally you run the sub-command 'holon metadata publish' to convert the folder containing the Holon MetaData DNA (can contain any number of files and sub-folders) into a OASIS Holon MetaData DNA file (.oholonmetadata) as well as optionally upload to STARNET.",
                "You can then share the .oholonmetadata file with others across any platform or OS, who can then install the Holon MetaData DNA from the file using the sub-command 'holon metadata install'.",
                "You can also optionally choose to upload the .oholonmetadata file to the STARNET store so others can search, download and install the Holon MetaData DNA."
            },
            STAR.STARDNA.DefaultHolonsMetaDataDNASourcePath, "DefaultHolonsMetaDataDNASourcePath",
            STAR.STARDNA.DefaultHolonsMetaDataDNAPublishedPath, "DefaultHolonsMetaDataDNAPublishedPath",
            STAR.STARDNA.DefaultHolonsMetaDataDNADownloadedPath, "DefaultHolonsMetaDataDNADownloadedPath",
            STAR.STARDNA.DefaultHolonsMetaDataDNAInstalledPath, "DefaultHolonsMetaDataDNAInstalledPath",
            50)
        { }
    }
}