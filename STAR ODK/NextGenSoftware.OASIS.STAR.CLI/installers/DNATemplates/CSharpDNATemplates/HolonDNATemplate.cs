using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates.Interfaces;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class HolonDNATemplate : Holon, IHolonDNATemplate
    {
        private static HolonDNATemplate _instance = null;
        public HolonDNATemplate() : base() { }
        //public HolonDNATemplate() : base(new Guid("ID")) { } //If you only plan to have one instance of this holon then un-comment this line and comment the above line.

        public static HolonDNATemplate Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HolonDNATemplate();
                
                return _instance;
            }
        }
    }
}