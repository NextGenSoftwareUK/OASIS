using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Email;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IEmailService
    {
        bool IsConfigured();
        Task<EmailResult> SendVerificationEmailAsync(EmailVerificationData data);
    }
}



