using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Crystal : ICrystal
    //public abstract class Crystal : ICrystal //TODO: Would rather it was virtual but MongoDB does not support this unless you apply this: https://stackoverflow.com/questions/57015856/invalidoperationexception-cant-compile-a-newexpression-with-a-constructor-decl but then that means need to create copies of the objects in MongoDB. Will do this later... :)
    {
        public EnumValue<CrystalName> Name { get; set; }
        public EnumValue<CrystalType> Type { get; set; }
        public string Description { get; set; }
        public int ProtectionLevel { get; set; }
        public int EnergisingLevel { get; set; }
        public int GroundingLevel { get; set; }
        public int CleansingLevel { get; set; }
        public int AmplifyicationLevel { get; set; }

        //TODO: Lots more to be added soon... ;-)
    }
}