
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class CellInfo
    {
        private CellInfoType _cellInfoType = CellInfoType.None;

        [Key("provisioned")]
        public ProvisionedCell Provisioned { get; set; }

        [Key("cloned")]
        public ClonedCell Cloned { get; set; }

        [Key("stem")]
        public StemCell Stem { get; set; }

        [IgnoreMember]
        public CellInfoType CellInfoType
        {
            get
            {
                if (_cellInfoType == CellInfoType.None)
                {
                    if (Provisioned != null)
                        _cellInfoType = CellInfoType.Provisioned;

                    else if (Cloned != null)
                        _cellInfoType = CellInfoType.Cloned;

                    else if (Stem != null)
                        _cellInfoType = CellInfoType.Stem;
                }

                return _cellInfoType;
            }
        }
    }
}