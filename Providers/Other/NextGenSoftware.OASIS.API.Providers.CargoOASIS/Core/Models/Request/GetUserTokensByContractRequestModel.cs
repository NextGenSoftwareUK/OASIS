namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetUserTokensByContractRequestModel
    {
        /// <summary>
        /// Optional. String. Page of results to display.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. Limit of results to display per page.
        /// </summary>
        public string Limit { get; set; }
        /// <summary>
        /// Required. String. ID of collection 
        /// </summary>
        public string ContractId { get; set; }
        /// <summary>
        /// Optional. String. Ethereum wallet address of user. Should set skipAuth option to true when using address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. Boolean. Skips using the current logged in users address and will use the address value
        /// </summary>
        public bool? SkipAuth { get; set; }

        public string AccessJwtToken { get; set; }
    }
}