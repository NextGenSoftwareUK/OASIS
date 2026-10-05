namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Email
{
    public class EmailVerificationData
    {
        public string To { get; set; }
        public string Username { get; set; }
        public string VerificationToken { get; set; }
        public string VerificationUrl { get; set; }
        public string Subject { get; set; }
    }
}



