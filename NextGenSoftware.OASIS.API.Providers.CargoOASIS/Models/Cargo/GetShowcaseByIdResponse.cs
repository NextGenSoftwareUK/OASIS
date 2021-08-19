using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class GetShowcaseByIdResponse
    {
        public string Name { get; set; }
        public string CreateAt { get; set; }
        public bool Public { get; set; }
        public bool ResellingEnabled { get; set; }
        public string SlugId { get; set; }
        public string Slug { get; set; }
        public bool IsOwner { get; set; }
        public bool IsVendor { get; set; }
        public Owner Owner { get; set; }
    }
}