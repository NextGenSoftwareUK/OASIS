namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request
{
    public class CreateAccountRequestModel : BaseConfigRequestModel
    {
        /// <summary>
        /// Optional. Valid email address that will be tied to the account
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Optional. Username to be used for new account
        /// </summary>
        public string UserName { get; set; }

        public string AccountAddress { get; set; }
    }
}