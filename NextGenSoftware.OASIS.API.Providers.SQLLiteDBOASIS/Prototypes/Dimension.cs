using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Prototypes{

    public class Dimension : Holon, IDimension
    {
        public new DimensionLevel DimensionLevel { get; set;}

        public Dimension(){}
    }
}