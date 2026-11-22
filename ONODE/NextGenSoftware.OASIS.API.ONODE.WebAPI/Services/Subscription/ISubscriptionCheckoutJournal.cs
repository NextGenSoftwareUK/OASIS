using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public interface ISubscriptionCheckoutJournal
{
    Task<string> BeginCheckoutAsync(string avatarId, string planId, CancellationToken token);
    Task RecordCheckoutSessionAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token);
    Task CloseCheckoutAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token);
}
