using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed record SubscriptionPortalSession(string Id, string Url);
public interface ISubscriptionBillingPortal
{
    Task<SubscriptionPortalSession> CreateAsync(SubscriptionRecord subscription, string returnUrl, CancellationToken token);
}
public sealed class StripeSubscriptionBillingPortal(IConfiguration configuration) : ISubscriptionBillingPortal
{
    public const string ApplicationMarker = "oasis-subscriptions";
    public async Task<SubscriptionPortalSession> CreateAsync(SubscriptionRecord subscription, string returnUrl, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(subscription?.StripeCustomerId))
            throw new InvalidOperationException("This subscriber has no Stripe billing account.");
        var client=new StripeClient(SubscriptionStripeConfiguration.SecretKey(configuration));
        var configurations=await new Stripe.BillingPortal.ConfigurationService(client).ListAsync(
            new Stripe.BillingPortal.ConfigurationListOptions { Active=true, Limit=100 }, cancellationToken:token);
        var matching=configurations.Data.Where(item=>item.Metadata?.GetValueOrDefault("oasis_application")==ApplicationMarker).ToList();
        if(matching.Count!=1)throw new InvalidOperationException("Configure exactly one active OASIS subscription customer portal in Stripe (metadata oasis_application=oasis-subscriptions).");
        var session=await new Stripe.BillingPortal.SessionService(client).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions {
            Customer=subscription.StripeCustomerId, Configuration=matching[0].Id, ReturnUrl=returnUrl
        }, cancellationToken:token);
        return new SubscriptionPortalSession(session.Id,session.Url);
    }
}
