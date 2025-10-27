namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetTokenDetailsRequestModel
    {
        /// <summary>
        /// The ID of the project on Cargo.
        /// This can be found in the URL bar when viewing the project on Cargo.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// The ID of the collectible within the collection.
        /// </summary>
        public string CollectibleId { get; set; }
    }
}