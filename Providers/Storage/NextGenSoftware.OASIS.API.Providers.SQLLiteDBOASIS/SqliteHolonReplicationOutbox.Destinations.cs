using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationDestinationBinding>> ReadDestinationBindingAsync(ProviderType targetAuthority,
        CancellationToken cancellationToken=default)
        =>Execute(()=>
        {
            CosmicConcurrency.ValidateAuthority(targetAuthority);
            if(_expectedSource==null)
                return new OASISResult<HolonReplicationDestinationBinding> {IsError=true,ErrorCount=1,
                    ErrorCode="COSMIC_DESTINATION_CATALOGUE_SOURCE_REQUIRED",Message="A native-bound source is required to read approved destinations."};
            if(targetAuthority==_expectedSource.Authority)throw new ArgumentException("A destination must use a different authority.",nameof(targetAuthority));
            using var connection=Open();using var transaction=connection.BeginTransaction(deferred:true);
            ValidateSource(connection,transaction);
            return new OASISResult<HolonReplicationDestinationBinding>(SqliteDestinationBindings.Read(connection,transaction,
                _expectedSource,targetAuthority,out _));
        },cancellationToken);
}
