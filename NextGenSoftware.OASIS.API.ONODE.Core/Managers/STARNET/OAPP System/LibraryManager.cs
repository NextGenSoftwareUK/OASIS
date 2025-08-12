using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class LibraryManager : STARNETManagerBase<Library, DownloadedLibrary, InstalledLibrary, LibraryDNA>
    public class LibraryManager : STARNETManagerBase<Library, DownloadedLibrary, InstalledLibrary, STARNETDNA>
    {
        public LibraryManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, 
            OASISDNA,
            typeof(LibraryType),
            HolonType.Library,
            HolonType.InstalledLibrary,
            "Library",
            "LibraryId",
            "LibraryName",
            "LibraryType",
            "olib",
            "oasis_libs",
            "LibraryDNA.json",
            "LibraryDNAJSON")
        { }

        public LibraryManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(LibraryType),
            HolonType.Library,
            HolonType.InstalledLibrary,
            "Library",
            "LibraryId",
            "LibraryName",
            "LibraryType",
            "olib",
            "oasis_libs",
            "LibraryDNA.json",
            "LibraryDNAJSON")
        { }
    }
}