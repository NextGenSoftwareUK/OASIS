namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetShowcaseBySlugRequestModel
    {
        /// <summary>
        /// Required. String. The slug of the showcase
        /// </summary>
        public string Slug { get; set; }
        /// <summary>
        /// Required. String. Slug ID of the showcase
        /// </summary>
        public string SlugId { get; set; }
    }
}