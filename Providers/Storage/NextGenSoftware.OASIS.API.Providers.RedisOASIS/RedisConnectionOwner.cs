#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS;

/// <summary>One reusable SDK connection per provider instance; no command backlog/replay policy.</summary>
internal sealed class RedisConnectionOwner : IDisposable
{
    private readonly ConfigurationOptions _options;
    private readonly SemaphoreSlim _lifecycle=new(1,1);
    private ConnectionMultiplexer? _client;
    private bool _disposed;

    public RedisConnectionOwner(string configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        _options=ConfigurationOptions.Parse(configuration);
        _options.AbortOnConnectFail=true;_options.ConnectRetry=0;_options.BacklogPolicy=BacklogPolicy.FailFast;
    }

    private async Task<ConnectionMultiplexer> ClientAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            _client??=await ConnectionMultiplexer.ConnectAsync(_options).ConfigureAwait(false);
            return _client;
        }
        finally{_lifecycle.Release();}
    }

    public async Task<IDatabase> DatabaseAsync()=>(await ClientAsync().ConfigureAwait(false)).GetDatabase();
    public async Task<IReadOnlyList<IServer>> PrimariesAsync()
    {
        var client=await ClientAsync().ConfigureAwait(false);
        return client.GetEndPoints().Select(endpoint=>client.GetServer(endpoint)).Where(server=>!server.IsReplica).ToArray();
    }

    public async Task RequireReviewedLayoutAsync()
    {
        var db=await DatabaseAsync().ConfigureAwait(false);var servers=await PrimariesAsync().ConfigureAwait(false);
        if(servers.Count==0 || servers.Any(server=>!server.IsConnected))throw new InvalidOperationException("Connected Redis primary discovery is required.");
        foreach(var server in servers)
            foreach(var pattern in new[]{"avatar:*","avatar-detail:*","holon:*","avatar-by-username:*","avatar-by-email:*","detail-by-username:*","detail-by-email:*","avatars:all","holons:all"})
                await foreach(var key in server.KeysAsync(db.Database,pattern,pageSize:1,flags:CommandFlags.DemandMaster).ConfigureAwait(false))
                    throw new InvalidOperationException("REDIS_LEGACY_LAYOUT_REVIEW_REQUIRED: legacy record/index keys exist. Preserve them and review migration; no alternate reader or automatic conversion is used.");
    }

    public Task CloseAsync()=>CloseAsync(false);
    private async Task CloseAsync(bool permanent)
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            if(permanent)_disposed=true;
            var client=_client;_client=null;
            if(client!=null)
                try{await client.CloseAsync(allowCommandsToComplete:true).ConfigureAwait(false);}
                finally{client.Dispose();}
        }
        finally{_lifecycle.Release();}
    }

    public void Dispose()=>CloseAsync(true).GetAwaiter().GetResult();
}
