using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    /// <summary>Trusted read-only audit of explicitly selected receipts; no HTTP authorization is granted.</summary>
    public Task<OASISResult<MongoReplicaReceiptMembershipAudit>> AuditReplicaReceiptMembershipAsync(Guid operatorId,IEnumerable<Guid> operations,CancellationToken token=default)
        =>IsProviderActivated && _holonRepository is HolonRepository repository
            ? repository.AuditReplicaReceiptMembershipAsync(operatorId,operations,token)
            :Task.FromResult(new OASISResult<MongoReplicaReceiptMembershipAudit>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_PROVIDER_NOT_ACTIVE",Message="Activate the intended native provider before auditing."});

    /// <summary>Explicit authorized maintenance only. Preserve/export the audited hashes and stable migration ID before submission.</summary>
    public Task<OASISResult<int>> MigrateReplicaReceiptMembershipAsync(Guid migrationId,MongoReplicaReceiptMembershipAudit audit,CancellationToken token=default)
        =>IsProviderActivated && _holonRepository is HolonRepository repository
            ? repository.MigrateReplicaReceiptMembershipAsync(migrationId,audit,token)
            :Task.FromResult(new OASISResult<int>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_PROVIDER_NOT_ACTIVE",Message="Activate the intended native provider before migration."});
}
