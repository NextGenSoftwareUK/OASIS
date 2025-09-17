namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetCollectiblesListByProjectIdRequestModel
    {
        /// <summary>
        /// Required. ID of the contract you wish to query
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// Optional. Optional page for pagination.
        /// </summary>
        public int? Page { get; set; }
        /// <summary>
        /// Optional. Limit the number of results returned. Defaults to 10.
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Optional. Only tokens owned by this address will be returned in the response.
        /// </summary>
        public string OwnerAddress { get; set; }
    }
}