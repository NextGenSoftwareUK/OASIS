using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class RuntimeDNA : IRuntimeDNA
    {
        public Guid Id { get; set; }
        //public string Name
        //{
        //    get
        //    {
        //        return string.Concat(Enum.GetName(typeof(RuntimeType), RuntimeType), " ", Version);
        //    }
        //}

        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        public DateTime CreatedOn { get; set; }

        public RuntimeType RuntimeType { get; set; }
        public string RuntimePath { get; set; }

        //public string InstalledPath { get; set; }

        //public DateTime InstalledOn { get; set; }

        //public Guid InstalledBy { get; set; }

        //public string InstalledByAvatarUsername { get; set; }


        public Guid PublishedByAvatarId { get; set; }
        public string PublishedByAvatarUsername { get; set; }
        public DateTime PublishedOn { get; set; }
        public string PublishedPath { get; set; }
        public bool RuntimePublishedOnSTARNET { get; set; }
        public bool RuntimePublishedToCloud { get; set; }
        public ProviderType RuntimePublishedProviderType { get; set; }
        public long RuntimeFileSize { get; set; }

        public bool IsActive { get; set; }
        public string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
        public string DotNetVersion { get; set; }

        public int Versions { get; set; }
        public int Downloads { get; set; }
    }
}
