using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class ZomeDNATemplate : ZomeBase, IZome
    {
        public ZomeDNATemplate() : base(new Guid("ID")) { }
    }
}