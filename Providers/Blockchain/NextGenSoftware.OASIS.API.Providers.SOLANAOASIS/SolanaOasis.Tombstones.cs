using System;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private async Task<(TResult Record,string Signature)> CreateVerifiedTombstoneAsync<T,TResult>(
        string original,Guid id,int currentVersion,Func<T,TResult> project) where T:SolanaBaseDto,new()
    {
        if(id==Guid.Empty || currentVersion<0) throw new InvalidDataException("Invalid Solana deletion identity/version.");
        var expectedVersion=checked(currentVersion+1);
        var signature=await _solanaRepository.CreateTombstoneAsync(original);
        try
        {
            var confirmed=await _solanaRepository.GetAsync<T>(signature);
            if(confirmed.Id!=id || confirmed.Version!=expectedVersion || confirmed.PreviousVersionId!=id
                || !confirmed.IsDeleted || confirmed.DeletedDate==DateTime.MinValue || confirmed.DeletedDate.Kind!=DateTimeKind.Utc)
                throw new InvalidDataException("Finalized Solana tombstone does not match the requested deletion.");
            return (project(confirmed),signature);
        }
        catch(Exception cause)
        {
            throw new SolanaTransactionReconciliationException(signature,cause);
        }
    }
}
