using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class CelestialBodies : STARUIBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody>
    {
        public CelestialBodies(Guid avatarId) : base(new API.ONODE.Core.Managers.CelestialBodyManager(avatarId),
            "Welcome to the Celestial Bodies Wizard", new List<string> 
            {
                "This wizard will allow you create a CelestialBody which contain Zomes's & Holon's.",
                "CelestialBody's can be anything from a Moon, Planet or Star (lot's more too!).",
                "The wizard will create an empty folder with a CelestialBodyDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'celestialbody publish' to convert the folder containing the celestialbody (can contain any number of files and sub-folders) into a OASIS CelestialBody file (.ocelestialbody) as well as optionally upload to STARNET.",
                "You can then share the .ocelestialbody file with others across any platform or OS, who can then install the CelestialBody from the file using the sub-command 'celestialbody install'.",
                "You can also optionally choose to upload the .ocelestialbody file to the STARNET store so others can search, download and install the celestialbody."
            },
            STAR.STARDNA.DefaultCelestialBodiesSourcePath, "DefaultCelestialBodiesSourcePath",
            STAR.STARDNA.DefaultCelestialBodiesPublishedPath, "DefaultCelestialBodiesPublishedPath",
            STAR.STARDNA.DefaultCelestialBodiesDownloadedPath, "DefaultCelestialBodiesDownloadedPath",
            STAR.STARDNA.DefaultCelestialBodiesInstalledPath, "DefaultCelestialBodiesInstalledPath")
        { }
    }
}