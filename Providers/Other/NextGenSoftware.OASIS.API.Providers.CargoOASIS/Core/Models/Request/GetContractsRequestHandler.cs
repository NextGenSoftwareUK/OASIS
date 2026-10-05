namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetContractsRequestHandler
    {
        /// <summary>
        /// Optional. String. Page of results to display. Defaults to 1.
        /// </summary>
        public string Page { get; set; }

        /// <summary>
        /// Optional. String. Limit of collections per page. Defaults to 10.
        /// </summary>
        public string Limit { get; set; }

        /// <summary>
        /// Optional. String. Limit results to show only collections in the given showcase.
        /// </summary>
        public string ShowcaseId { get; set; }

        /// <summary>
        /// Optional. Boolean. Show only collections that the current authenticated user owns.
        /// </summary>
        public bool? Owned { get; set; }

        /// <summary>
        /// Optional. String. Ethereum wallet address. If specified will only return collections for a given user.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Optional. Boolean. Will not use current logged in users address if true.
        /// </summary>
        public bool? SkipAuth { get; set; }

        /// <summary>
        /// Optional. Boolean. Will only return collection if the user owns at least one NFT within that collection.
        /// </summary>
        public bool? HasTokens { get; set; }

        /// <summary>
        /// Optional. Boolean. Return only collections created on Cargo.
        /// </summary>
        public bool? CargoContract { get; set; }

        /// <summary>
        /// Optional. Boolean. Show only collections that the current authenticated user either owns,
        /// or has collectibles in.
        /// Collections within response will contain an additional contractTokens property stating how many collectibles
        /// the user owns within that collection. This takes precedence over the owned parameter.
        /// </summary>
        public bool? UseAuthToken { get; set; }

        public string AccessJwtToken { get; set; }
    }
}