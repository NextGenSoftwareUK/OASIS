using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class CelestialBodiesMetaDataDNA : STARNETUIBase<CelestialBodyMetaDataDNA, DownloadedCelestialBodyMetaDataDNA, InstalledCelestialBodyMetaDataDNA, STARNETDNA>
    {
        public CelestialBodiesMetaDataDNA(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.CelestialBodyMetaDataDNAManager(avatarId, STARDNA),
            "Welcome to the Celestial Body MetaData DNA Wizard", new List<string> 
            {
                "This wizard will allow you create CelestialBody MetaData DNA which contain MetaData for Zomes's & Holon's.",
                "CelestialBody MetaData DNA contains Zome MetaData DNA which in turn contains Holon MetaData DNA.",
                "The wizard will create an empty folder with a CelestialBodyMetaDataDNA.json file in it. You then simply place the CelestialBody MetaData DNA and any other files/folders you need for the assets (optional) for the Celestial Body MetaData DNA into this folder.",
                "Finally you run the sub-command 'celestialbody metadata publish' to convert the folder containing the Celestial Body MetaData DNA (can contain any number of files and sub-folders) into a OASIS Celestial Body MetaData DNA file (.ocelestialbodymetadata) as well as optionally upload to STARNET.",
                "You can then share the .ocelestialbodymetadata file with others across any platform or OS, who can then install the CelestialBody from the file using the sub-command 'celestialbody metadata install'.",
                "You can also optionally choose to upload the .ocelestialbodymetadata file to the STARNET store so others can search, download and install the Celestial Body MetaData DNA."
            },
            STAR.STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath, "DefaultCelestialBodiesMetaDataDNASourcePath",
            STAR.STARDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath, "DefaultCelestialBodiesMetaDataDNAPublishedPath",
            STAR.STARDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath, "DefaultCelestialBodiesMetaDataDNADownloadedPath",
            STAR.STARDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath, "DefaultCelestialBodiesMetaDataDNAInstalledPath")
        { }
    }
}