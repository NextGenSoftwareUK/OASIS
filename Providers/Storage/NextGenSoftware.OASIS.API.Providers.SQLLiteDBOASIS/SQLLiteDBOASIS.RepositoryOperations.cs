using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS
{
    // The provider owns the store identity and memory-store keeper. A repository
    // operation owns its EF context, including awaited I/O and transaction cleanup.
    // Concurrent requests never share a context or its change tracker.
    private T WithAvatarRepository<T>(Func<AvatarRepository,T> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return operation(new AvatarRepository(context));
    }
    private async Task<T> WithAvatarRepositoryAsync<T>(Func<AvatarRepository,Task<T>> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return await operation(new AvatarRepository(context)).ConfigureAwait(false);
    }
    private T WithAvatarDetailRepository<T>(Func<AvatarDetailRepository,T> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return operation(new AvatarDetailRepository(context));
    }
    private async Task<T> WithAvatarDetailRepositoryAsync<T>(Func<AvatarDetailRepository,Task<T>> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return await operation(new AvatarDetailRepository(context)).ConfigureAwait(false);
    }
    private T WithHolonRepository<T>(Func<HolonRepository,T> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return operation(new HolonRepository(context,()=>IsVersionControlEnabled));
    }
    private async Task<T> WithHolonRepositoryAsync<T>(Func<HolonRepository,Task<T>> operation)
    {
        using var context=new DataContext(_ownConnectionString);
        return await operation(new HolonRepository(context,()=>IsVersionControlEnabled)).ConfigureAwait(false);
    }
}
