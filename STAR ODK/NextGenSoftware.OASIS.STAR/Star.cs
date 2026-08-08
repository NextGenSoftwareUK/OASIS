using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

using NextGenSoftware.OASIS.STAR.Interfaces;
using SevenZip.Buffer;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace NextGenSoftware.OASIS.STAR
{
    public static partial class STAR
    {
        // const string STAR_DNA_DEFAULT_PATH = "DNA\\STAR_DNA.json";
        // const string OASIS_DNA_DEFAULT_PATH = "DNA\\OASIS_DNA.json";
        const string STAR_DNA_DEFAULT_PATH = "DNA/STAR_DNA.json";
        const string OASIS_DNA_DEFAULT_PATH = "DNA/OASIS_DNA.json";

        private static StarStatus _status;
        private static Guid _starId = Guid.Empty;
        private static OASISAPI _OASISAPI = null;
        private static STARAPI _STARAPI = null;
        private static IPlanet _defaultPlanet = null;
        private static ISuperStar _defaultSuperStar = null;
        private static IGrandSuperStar _defaultGrandSuperStar = null;
        private static IGreatGrandSuperStar _defaultGreatGrandSuperStar = null;

    }
}