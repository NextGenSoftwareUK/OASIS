namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetProjectMetadataRequestModel
    {
        /// <summary>
        /// Required. The ID of the project. This can be found in the URL bar when viewing the project on Cargo.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// Optional. If true this method requires authentication. Will return isOwned boolean in response
        /// </summary>
        public bool? UseAuth { get; set; }

        public string AccessJwtToken { get; set; }
    }
}