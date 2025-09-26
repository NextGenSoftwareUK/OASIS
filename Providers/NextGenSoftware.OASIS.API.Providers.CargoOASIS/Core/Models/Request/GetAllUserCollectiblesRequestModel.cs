namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetAllUserCollectiblesRequestModel
    {
        /// <summary>
        /// Required. String. The Ethereum wallet to fetch NFTs for.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. String. The page used for pagination.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. The max number of results to return.
        /// </summary>
        public string Limit { get; set; }
    }
}