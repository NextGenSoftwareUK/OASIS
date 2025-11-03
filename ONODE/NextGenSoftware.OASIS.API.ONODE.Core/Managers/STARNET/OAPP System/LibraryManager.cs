using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class LibraryManager : STARNETManagerBase<Library, DownloadedLibrary, InstalledLibrary, LibraryDNA>
    public class LibraryManager : STARNETManagerBase<Library, DownloadedLibrary, InstalledLibrary, STARNETDNA>
    {
        public LibraryManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(LibraryType),
            HolonType.Library,
            HolonType.InstalledLibrary,
            "Library",
            //"LibraryId",
            "STARNETHolonId",
            "LibraryName",
            "LibraryType",
            "olib",
            "oasis_libs",
            "LibraryDNA.json",
            "LibraryDNAJSON")
        { }

        public LibraryManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(LibraryType),
            HolonType.Library,
            HolonType.InstalledLibrary,
            "Library",
            //"LibraryId",
            "STARNETHolonId",
            "LibraryName",
            "LibraryType",
            "olib",
            "oasis_libs",
            "LibraryDNA.json",
            "LibraryDNAJSON")
        { }
    }
}