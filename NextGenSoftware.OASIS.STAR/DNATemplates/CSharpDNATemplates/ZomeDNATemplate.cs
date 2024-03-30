using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates.Interfaces;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class ZomeDNATemplate : ZomeBase, IZome
    {
        public ZomeDNATemplate() : base(new Guid("ID")) { }
    }
}