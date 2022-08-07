using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class ZomeDNATemplate : ZomeBase, IZome
    {
        public ZomeDNATemplate() : base(new Guid("ID")) { }
    }
}