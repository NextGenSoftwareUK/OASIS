using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public interface IPaidOverageGateway
{
    Task<PaidOverageInvoiceResult> CollectAsync(PaidOverageStatement statement,Func<string,Task> attachInvoice,CancellationToken token);
    Task VerifyPaymentReadyAsync(SubscriptionRecord subscriber,CancellationToken token);
    Task<PaidOverageInvoiceResult> GetAsync(PaidOverageStatement statement,string invoiceId,CancellationToken token);
}
public sealed class StripePaidOverageGateway(IConfiguration configuration) : IPaidOverageGateway
{
    public const string CostBasis="overage_charge_usd_v1";
    private StripeClient Client() => new(SubscriptionStripeConfiguration.SecretKey(configuration));
    private static RequestOptions Options(string id,string action) => new(){IdempotencyKey=id+":"+action};
    public async Task VerifyPaymentReadyAsync(SubscriptionRecord subscriber,CancellationToken token)
    {
        var client=Client();
        var canonical=await new Stripe.SubscriptionService(client).GetAsync(subscriber.StripeSubscriptionId,cancellationToken:token);
        if(canonical.CustomerId!=subscriber.StripeCustomerId || canonical.Status is not ("active" or "trialing") ||
            canonical.Metadata?.GetValueOrDefault("avatar_id") is string owner && !string.IsNullOrEmpty(owner) && owner!=subscriber.UserId)
            throw new InvalidOperationException("The paid Stripe subscription must be active and belong to this avatar.");
        string method=canonical.DefaultPaymentMethodId;
        if(string.IsNullOrWhiteSpace(method))method=(await new CustomerService(client).GetAsync(subscriber.StripeCustomerId,cancellationToken:token)).InvoiceSettings?.DefaultPaymentMethodId;
        if(string.IsNullOrWhiteSpace(method))throw new InvalidOperationException("Add a default payment method in Manage billing before enabling overages.");
    }
    public async Task<PaidOverageInvoiceResult> CollectAsync(PaidOverageStatement statement,Func<string,Task> attachInvoice,CancellationToken token)
    {
        if(statement.AmountMinorUsd==0)return new(null,"paid",null,0,0,false);
        var client=Client();var invoices=new InvoiceService(client);Invoice invoice=null;
        if(statement.StripeInvoiceId!=null)invoice=await invoices.GetAsync(statement.StripeInvoiceId,cancellationToken:token);
        else
        {
            // Recover by immutable metadata even after Stripe's 24-hour idempotency cache expires.
            await foreach(var candidate in invoices.ListAutoPagingAsync(new InvoiceListOptions{Customer=statement.StripeCustomerId,Limit=100},cancellationToken:token))
                if(candidate.Metadata?.GetValueOrDefault("oasis_overage_statement_id")==statement.Id && candidate.Metadata?.GetValueOrDefault("oasis_ledger_scope")==statement.Scope)
                {
                    if(invoice!=null)throw new InvalidOperationException("Multiple Stripe invoices reference this immutable overage statement; reconcile them before collection.");
                    invoice=candidate;
                }
            if(invoice==null)
            {
                var subscription=await new Stripe.SubscriptionService(client).GetAsync(statement.StripeSubscriptionId,cancellationToken:token);
                if(subscription.CustomerId!=statement.StripeCustomerId)throw new InvalidOperationException("The Stripe subscription/customer owner changed.");
                string method=subscription.DefaultPaymentMethodId;
                if(string.IsNullOrWhiteSpace(method))method=(await new CustomerService(client).GetAsync(statement.StripeCustomerId,cancellationToken:token)).InvoiceSettings?.DefaultPaymentMethodId;
                if(statement.AmountMinorUsd>0 && string.IsNullOrWhiteSpace(method))throw new InvalidOperationException("A default Stripe payment method is required to collect the agreed USD overages.");
                invoice=await invoices.CreateAsync(new InvoiceCreateOptions {Customer=statement.StripeCustomerId,Currency="usd",AutoAdvance=false,
                    CollectionMethod="charge_automatically",DefaultPaymentMethod=method,PendingInvoiceItemsBehavior="exclude",
                    Description="OASIS USD overages for "+statement.Month+" (separate from the monthly subscription)",
                    Metadata=new(){{"oasis_overage_statement_id",statement.Id},{"oasis_ledger_scope",statement.Scope},{"oasis_avatar_id",statement.UserId},
                        {"oasis_usage_month",statement.Month},{"oasis_usage_cost_basis",CostBasis}}},Options(statement.Id,"create"),token);
            }
            await attachInvoice(invoice.Id);
        }
        ValidateIdentity(statement,invoice);
        if(invoice.Status=="draft")
        {
            if(invoice.Lines.Data.Count==0)
                await new InvoiceItemService(client).CreateAsync(new InvoiceItemCreateOptions {Customer=statement.StripeCustomerId,Invoice=invoice.Id,
                    Currency="usd",Amount=statement.AmountMinorUsd,
                    Description="Extra requests USD "+statement.RequestChargesUsd.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                        "; AI usage and credits USD "+statement.AiChargesUsd.ToString(System.Globalization.CultureInfo.InvariantCulture)+"; no AI margin",
                    Metadata=new(){{"oasis_overage_statement_id",statement.Id}}},Options(statement.Id,"line"),token);
            invoice=await invoices.GetAsync(invoice.Id,cancellationToken:token);
            if(invoice.Subtotal!=statement.AmountMinorUsd)throw new InvalidOperationException("The Stripe draft subtotal differs from its immutable USD overage statement.");
            invoice=await invoices.FinalizeInvoiceAsync(invoice.Id,new InvoiceFinalizeOptions {AutoAdvance=true},Options(statement.Id,"finalize"),token);
        }
        if(invoice.Status=="open" && !invoice.Attempted)
        {
            try {invoice=await invoices.PayAsync(invoice.Id,new InvoicePayOptions(),Options(statement.Id,"pay"),token);}
            catch(StripeException)
            {
                invoice=await invoices.GetAsync(invoice.Id,cancellationToken:token);
                if(!invoice.Attempted && invoice.Status!="paid")throw;
            }
        }
        return Result(statement,invoice);
    }
    public async Task<PaidOverageInvoiceResult> GetAsync(PaidOverageStatement statement,string invoiceId,CancellationToken token)
        => Result(statement,await new InvoiceService(Client()).GetAsync(invoiceId,cancellationToken:token));
    private static void ValidateIdentity(PaidOverageStatement statement,Invoice invoice)
    {
        if(invoice.CustomerId!=statement.StripeCustomerId || invoice.Currency!="usd" || invoice.SubscriptionId!=null ||
            invoice.Metadata?.GetValueOrDefault("oasis_overage_statement_id")!=statement.Id ||
            invoice.Metadata?.GetValueOrDefault("oasis_ledger_scope")!=statement.Scope ||
            invoice.Metadata?.GetValueOrDefault("oasis_avatar_id")!=statement.UserId ||
            invoice.Metadata?.GetValueOrDefault("oasis_usage_month")!=statement.Month ||
            invoice.Metadata?.GetValueOrDefault("oasis_usage_cost_basis")!=CostBasis)
            throw new InvalidOperationException("Stripe invoice does not belong to this immutable standalone USD overage statement.");
    }
    private static PaidOverageInvoiceResult Result(PaidOverageStatement statement,Invoice invoice)
    {
        ValidateIdentity(statement,invoice);
        if(invoice.Subtotal!=statement.AmountMinorUsd)throw new InvalidOperationException("Finalized Stripe subtotal differs from the approved USD overage statement.");
        return new(invoice.Id,invoice.Status,invoice.HostedInvoiceUrl,invoice.Subtotal,invoice.AmountPaid,invoice.Attempted);
    }
}
