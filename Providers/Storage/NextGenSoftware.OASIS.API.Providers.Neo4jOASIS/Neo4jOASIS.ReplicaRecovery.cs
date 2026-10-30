using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICommittedHolonReplicaInitializationRecoveryStorageProvider
{
    public async Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken token=default)
    {
        try
        {
            token.ThrowIfCancellationRequested();var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(original,Authority.Neo4jOASIS);
            if(!IsProviderActivated || _driver==null)throw new InvalidOperationException("Activate the bound provider before receipt inspection.");
            using var session=_driver.AsyncSession();
            var rows=await(await session.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r LIMIT 2",new{operation=plan.OperationId.ToString("D")})).ToListAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();if(rows.Count>1)throw new InvalidDataException("Initialization receipt identity is ambiguous.");
            string digest=null;
            if(rows.Count==1)
            {
                var receipt=rows[0]["r"].As<INode>();
                if(!receipt.Properties.TryGetValue("Fingerprint",out var fingerprint) || fingerprint is not string bound || bound!=plan.Fingerprint
                    || !receipt.Properties.TryGetValue("ResultJson",out var body) || body is not string json)
                    throw new InvalidDataException("Initialization receipt does not match the original intent.");
                _=ConditionalHolonReceipt.Read(json,plan);digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            }
            return new(new(plan.OperationId,Authority.Neo4jOASIS,digest==null ? ConditionalHolonReceiptState.Absent:ConditionalHolonReceiptState.Committed,plan.Fingerprint,digest)){IsLoaded=true};
        }
        catch(Exception cause){return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException
            ? "COSMIC_INITIALIZATION_STATUS_CANCELLED":"COSMIC_INITIALIZATION_STATUS_UNVERIFIED",Message="The original initialization receipt could not be verified.",Exception=cause};}
    }
}
