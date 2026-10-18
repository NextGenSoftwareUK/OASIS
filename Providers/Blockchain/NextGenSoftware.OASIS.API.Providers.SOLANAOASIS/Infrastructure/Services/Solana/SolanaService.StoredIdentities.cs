using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed partial class SolanaService
{
    private readonly ISolanaRepository _storageRepository=storageRepository ?? new SolanaRepository(oasisAccount,rpcClient);

    public Task<OASISResult<SolanaAvatarDto>> GetAvatarByIdAsync(Guid id)
        => ReadStoredIdentityAsync<SolanaAvatarDto>(id!=Guid.Empty,row=>row.Id==id);
    public Task<OASISResult<SolanaAvatarDto>> GetAvatarByUsernameAsync(string username)
        => ReadStoredIdentityAsync<SolanaAvatarDto>(!string.IsNullOrWhiteSpace(username),row=>string.Equals(row.UserName,username,StringComparison.OrdinalIgnoreCase));
    public Task<OASISResult<SolanaAvatarDto>> GetAvatarByEmailAsync(string email)
        => ReadStoredIdentityAsync<SolanaAvatarDto>(!string.IsNullOrWhiteSpace(email),row=>string.Equals(row.Email,email,StringComparison.OrdinalIgnoreCase));
    public Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByIdAsync(Guid id)
        => ReadStoredIdentityAsync<SolanaAvatarDetailDto>(id!=Guid.Empty,row=>row.Id==id);
    public Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByUsernameAsync(string username)
        => ReadStoredIdentityAsync<SolanaAvatarDetailDto>(!string.IsNullOrWhiteSpace(username),row=>string.Equals(row.Username,username,StringComparison.OrdinalIgnoreCase));
    public Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByEmailAsync(string email)
        => ReadStoredIdentityAsync<SolanaAvatarDetailDto>(!string.IsNullOrWhiteSpace(email),row=>string.Equals(row.Email,email,StringComparison.OrdinalIgnoreCase));

    private async Task<OASISResult<T>> ReadStoredIdentityAsync<T>(bool valid,Func<T,bool> matches) where T:SolanaBaseDto,new()
    {
        var result=new OASISResult<T>();
        if(!valid) return new OASISResult<T> {IsError=true,ErrorCount=1,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Message="A nonempty Solana identity selector is required."};
        try
        {
            var records=await _storageRepository.GetAllAsync<T>();
            var selected=records.Where(entry=>matches(entry.Record)).Take(2).ToList();
            if(selected.Count>1) throw new StorageIdentityAmbiguousException();
            if(selected.Count==0) throw new KeyNotFoundException("No authenticated Solana record matches the requested identity.");
            var entry=selected[0];
            entry.Record.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS]=entry.Signature;
            result.Result=entry.Record;result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Solana stored identity lookup failed: "+cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is KeyNotFoundException) result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }

    private OASISResult<T> HandleError<T>(string message,Exception cause=null)
    {
        var result=new OASISResult<T>();
        OASISErrorHandling.HandleError(ref result,message,cause);
        return result;
    }
}
