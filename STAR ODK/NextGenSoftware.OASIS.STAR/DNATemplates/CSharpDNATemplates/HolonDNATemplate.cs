using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates.Interfaces;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class HolonDNATemplate : Holon, IHolonDNATemplate
    {
        public HolonDNATemplate() : base() { }
        //public HolonDNATemplate() : base(new Guid("ID")) { } //If you only plan to have one instance of this holon then un-comment this line and comment the above line.
    }
}