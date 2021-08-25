namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class GetUserShowcaseArgs
    {
        public string Page { get; set; }
        public string Limit { get; set; }
        public bool UseAuth { get; set; }
        public string Account { get; set; }
    }
}