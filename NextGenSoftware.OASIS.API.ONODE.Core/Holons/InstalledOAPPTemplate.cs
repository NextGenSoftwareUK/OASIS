using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    //public class InstalledOAPPTemplate : OAPPTemplate, IInstalledOAPPTemplate
    public class InstalledOAPPTemplate : InstalledOAPPSystemHolon, IInstalledOAPPTemplate
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

        //[CustomOASISProperty]
        //public Guid DownloadedOAPPTemplateHolonId { get; set; }

        //[CustomOASISProperty]
        //public Guid DownloadedOAPPSystemHolonId { get; set; } //TODO: Same as above (needed because of the generic interface IInstalledOAPPSystemHolon). Need to find way of renaming or hiding the base generic properties?

        //[CustomOASISProperty]
        //public string DownloadedPath { get; set; }

        //[CustomOASISProperty]
        //public DateTime DownloadedOn { get; set; }

        //[CustomOASISProperty]
        //public Guid DownloadedBy { get; set; }

        //[CustomOASISProperty]
        //public string DownloadedByAvatarUsername { get; set; }

        //[CustomOASISProperty]
        //public string InstalledPath { get; set; }

        //[CustomOASISProperty]
        //public DateTime InstalledOn { get; set; }

        //[CustomOASISProperty]
        //public Guid InstalledBy { get; set; }

        //[CustomOASISProperty]
        //public string InstalledByAvatarUsername { get; set; }

        //[CustomOASISProperty]
        //public DateTime UninstalledOn { get; set; }

        //[CustomOASISProperty]
        //public Guid UninstalledBy { get; set; }

        //[CustomOASISProperty]
        //public string UninstalledByAvatarUsername { get; set; }

        //[CustomOASISProperty]
        //public string Active { get; set; }

    }
}