using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class CelestialSpaces : STARNETUIBase<STARCelestialSpace, DownloadedCelestialSpace, InstalledCelestialSpace, CelestialSpaceDNA>
    public class CelestialSpaces : STARNETUIBase<STARCelestialSpace, DownloadedCelestialSpace, InstalledCelestialSpace, STARNETDNA>
    {
        public CelestialSpaces(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.CelestialSpaceManager(avatarId, STARDNA),
            "Welcome to the Celestial Spaces Wizard", new List<string> 
            {
                "This wizard will allow you create a CelestialSpace which contain CelestialBodies's.",
                "CelestialSpace can be anything from a Solar System to a Galaxy or Universe for example.",
                "The wizard will create an empty folder with a CelestialSpaceDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the celestialspace into this folder.",
                "Finally you run the sub-command 'celestialspace publish' to convert the folder containing the celestialspace (can contain any number of files and sub-folders) into a OASIS CelestialSpace file (.ocelestialspace) as well as optionally upload to STARNET.",
                "You can then share the .ocelestialspace file with others across any platform or OS, who can then install the CelestialSpace from the file using the sub-command 'celestialspace install'.",
                "You can also optionally choose to upload the .ocelestialspace file to the STARNET store so others can search, download and install the celestialspace."
            },
            STAR.STARDNA.DefaultCelestialSpacesSourcePath, "DefaultCelestialSpacesSourcePath",
            STAR.STARDNA.DefaultCelestialSpacesPublishedPath, "DefaultCelestialSpacesPublishedPath",
            STAR.STARDNA.DefaultCelestialSpacesDownloadedPath, "DefaultCelestialSpacesDownloadedPath",
            STAR.STARDNA.DefaultCelestialSpacesInstalledPath, "DefaultCelestialSpacesInstalledPath")
        { }
    }
}