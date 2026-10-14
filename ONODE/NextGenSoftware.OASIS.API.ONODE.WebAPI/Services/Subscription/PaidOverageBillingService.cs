using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class PaidOverageBillingService(IPaidOverageStatementStore store,IPaidOverageGateway gateway,ILogger<PaidOverageBillingService> logger,TimeProvider clock=null)
{
    public async Task<PaidOverageStatement> CollectAsync(PaidOverageStatement statement,CancellationToken token)
    {
        if(statement==null)return null;
        string owner=Guid.NewGuid().ToString("D");
        var claimed=await store.ClaimAsync(statement.Id,owner,(clock ?? TimeProvider.System).GetUtcNow().UtcDateTime,token);
        if(claimed==null)return await store.GetStatementAsync(statement.Id,token);
        try
        {
            var result=await gateway.CollectAsync(claimed,id=>store.AttachInvoiceAsync(claimed.Id,owner,id,token),token);
            await store.RecordInvoiceAsync(claimed.Id,owner,result,null,null,token);
        }
        catch(Exception ex)
        {
            await store.RetryAsync(claimed.Id,owner,ex.GetType().Name+": "+ex.Message,CancellationToken.None);
            logger.LogError(ex,"OVERAGE_COLLECTION_RETRY statement={StatementId}",claimed.Id);
            throw;
        }
        return await store.GetStatementAsync(statement.Id,token);
    }
    public async Task RunCycleAsync(DateTime now,CancellationToken token)
    {
        foreach(var account in await store.GetClosedAccountsAsync(now,token))
        {
            try {await store.PrepareAsync(account.UserId,account.Month,"monthly:"+Guid.NewGuid().ToString("D"),token);}
            catch(Exception ex) when(ex is not OperationCanceledException){logger.LogError(ex,"OVERAGE_STATEMENT_PREPARATION_FAILED user={UserId} month={Month}",account.UserId,account.Month);}
        }
        foreach(var statement in await store.GetReadyAsync(now,token))
        {
            try {await CollectAsync(statement,token);}
            catch(Exception ex) when(ex is not OperationCanceledException){logger.LogError(ex,"OVERAGE_STATEMENT_COLLECTION_PENDING statement={StatementId}",statement.Id);}
        }
    }
}
public sealed class PaidOverageBillingWorker(PaidOverageBillingService billing,ILogger<PaidOverageBillingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while(!token.IsCancellationRequested)
        {
            try {await billing.RunCycleAsync(DateTime.UtcNow,token);}
            catch(OperationCanceledException) when(token.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"OVERAGE_BILLING_CYCLE_FAILED");}
            await Task.Delay(TimeSpan.FromSeconds(60),token);
        }
    }
}
