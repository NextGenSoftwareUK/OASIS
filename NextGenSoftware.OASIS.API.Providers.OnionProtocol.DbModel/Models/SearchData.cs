using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Entity;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models
{
    public class SearchData : BaseEntity
    {
        public string Search_Id { get; set; }

        public string Data { get; set; }
    }
}