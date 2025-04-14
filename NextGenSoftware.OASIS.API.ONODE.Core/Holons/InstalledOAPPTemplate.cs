using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class InstalledOAPPTemplate : OAPPTemplate, IInstalledOAPPTemplate //: Holon, IInstalledOAPPTemplate //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        //private IDownloadedOAPPTemplate _DownloadedOAPPTemplate;

        public InstalledOAPPTemplate()
        {
            this.HolonType = HolonType.InstalledOAPPTemplate;
        }

        //[CustomOASISProperty]
        //public Guid OAPPId { get; set; }

        // [CustomOASISProperty(StoreAsJsonString = true)] //TODO: Get this working later on so we dont need to do the manual code below.
        //public IOAPPDNA OAPPDNA { get; set; }

        //public IDownloadedOAPPTemplate DownloadedOAPPTemplate { get; set; }

        //public IDownloadedOAPPTemplate DownloadedOAPPTemplate
        //{
        //    get
        //    {
        //        if (_DownloadedOAPPTemplate == null && MetaData["DownloadedOAPPTempateJSON"] != null && !string.IsNullOrEmpty(MetaData["DownloadedOAPPTempateJSON"].ToString()))
        //            _DownloadedOAPPTemplate = JsonSerializer.Deserialize<IDownloadedOAPPTemplate>(MetaData["DownloadedOAPPTempateJSON"].ToString());

        //        return _DownloadedOAPPTemplate;
        //    }
        //    set
        //    {
        //        _DownloadedOAPPTemplate = value;
        //        MetaData["DownloadedOAPPTempateJSON"] = JsonSerializer.Serialize(value);
        //    }
        //}

        [CustomOASISProperty]
        public Guid DownloadedOAPPTemplateId { get; set; }

        [CustomOASISProperty]
        public string DownloadedPath { get; set; }

        [CustomOASISProperty]
        public DateTime DownloadedOn { get; set; }

        [CustomOASISProperty]
        public Guid DownloadedBy { get; set; }

        [CustomOASISProperty]
        public string DownloadedByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public string InstalledPath { get; set; }

        [CustomOASISProperty]
        public DateTime InstalledOn { get; set; }

        [CustomOASISProperty]
        public Guid InstalledBy { get; set; }

        [CustomOASISProperty]
        public string InstalledByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public DateTime UninstalledOn { get; set; }

        [CustomOASISProperty]
        public Guid UninstalledBy { get; set; }

        [CustomOASISProperty]
        public string UninstalledByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public string Active { get; set; }

        //[CustomOASISProperty]
        //public string OAPPTemplateVersion { get; set; }
    }
}