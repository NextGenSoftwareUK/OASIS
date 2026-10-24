#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS;

/// <summary>Native Redis transport only; record/history/query/encryption policy belongs to shared storage.
/// Conditional revisions encode the exact observed native bytes and key, not a fabricated version counter.
/// These are state comparisons (like content ETags), not monotonic revisions or multi-key transactions.</summary>
internal sealed class RedisKeyValueBackend(
    Func<Task<IDatabase>> database,
    Func<Task<IReadOnlyList<IServer>>> primaries,
    string keyPrefix) : IConditionalKeyValueBackend
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private const string RevisionPrefix = "redis-state-v1:";
    private const string ReplaceScript = "if redis.call('GET',KEYS[1]) ~= ARGV[1] then return 0 end redis.call('SET',KEYS[1],ARGV[2]) return 1";
    private const string DeleteScript = "if redis.call('GET',KEYS[1]) ~= ARGV[1] then return 0 end return redis.call('DEL',KEYS[1])";

    private string Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if(string.IsNullOrEmpty(keyPrefix) || !key.StartsWith(keyPrefix,StringComparison.Ordinal))
            throw new ArgumentException("Redis storage key is outside the configured namespace.",nameof(key));
        return key;
    }

    public async Task<string> GetAsync(string key,CancellationToken cancellationToken=default)
        => (await ReadObservedAsync(key,cancellationToken))?.Value!;

    public async Task<KeyValueObservation?> ReadObservedAsync(string key,CancellationToken cancellationToken=default)
    {
        key=Key(key);cancellationToken.ThrowIfCancellationRequested();
        var db=await database();
        var value=await db.StringGetAsync(key,CommandFlags.DemandMaster);
        if(value.IsNull)return null;
        var bytes=(byte[])value!;
        var text=Utf8.GetString(bytes);
        // Bind the observed state to its exact key; no caller-created revision/alternate parsing.
        return new KeyValueObservation(text,RevisionPrefix+Convert.ToBase64String(Utf8.GetBytes(key))+":"+Convert.ToBase64String(bytes));
    }

    private byte[] Expected(string key,string revision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        var header=RevisionPrefix+Convert.ToBase64String(Utf8.GetBytes(key))+":";
        if(!revision.StartsWith(header,StringComparison.Ordinal))throw new ArgumentException("Redis observation belongs to another key or format.",nameof(revision));
        var encoded=revision.Substring(header.Length);
        var bytes=Convert.FromBase64String(encoded);
        if(Convert.ToBase64String(bytes)!=encoded)throw new ArgumentException("Redis observation is not canonical.",nameof(revision));
        return bytes;
    }

    public Task PutAsync(string key,string value,CancellationToken cancellationToken=default)
        => SetAsync(key,value,false,cancellationToken);
    public Task CreateOnlyAsync(string key,string value,CancellationToken cancellationToken=default)
        => SetAsync(key,value,true,cancellationToken);
    private async Task SetAsync(string key,string value,bool create,CancellationToken token)
    {
        key=Key(key);ArgumentNullException.ThrowIfNull(value);token.ThrowIfCancellationRequested();
        var bytes=Utf8.GetBytes(value);var db=await database();token.ThrowIfCancellationRequested();
        try
        {
            var acknowledged=await db.StringSetAsync(key,bytes,when:create?When.NotExists:When.Always,flags:CommandFlags.DemandMaster);
            if(!acknowledged)
            {
                if(create)throw new StorageLifecycleConflictException("Redis create-only key already exists.");
                throw new InvalidDataException("Redis SET lacks an authoritative acknowledgement.");
            }
        }
        catch(StorageLifecycleConflictException){throw;}
        catch(RedisServerException){throw;} // Native single-command rejection; no preceding write in this operation.
        catch(Exception cause){throw new RedisMutationReconciliationException(key,"SET",cause);}
    }

    public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken cancellationToken=default)
    {
        key=Key(key);ArgumentNullException.ThrowIfNull(value);
        return ConditionalAsync(key,Expected(key,revision),Utf8.GetBytes(value),false,cancellationToken);
    }
    public Task DeleteUnchangedAsync(string key,string revision,CancellationToken cancellationToken=default)
    {
        key=Key(key);return ConditionalAsync(key,Expected(key,revision),null,true,cancellationToken);
    }
    private async Task ConditionalAsync(string key,byte[] expected,byte[]? value,bool delete,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();var db=await database();token.ThrowIfCancellationRequested();
        try
        {
            var response=await db.ScriptEvaluateAsync(delete?DeleteScript:ReplaceScript,new RedisKey[]{key},
                delete?new RedisValue[]{expected}:new RedisValue[]{expected,value!},CommandFlags.DemandMaster);
            if(response.Type!=ResultType.Integer)throw new InvalidDataException("Redis conditional mutation omitted its integer acknowledgement.");
            var outcome=(long)response;
            if(outcome==0)throw new StorageLifecycleConflictException("Redis observed state changed or disappeared.");
            if(outcome!=1)throw new InvalidDataException("Redis conditional mutation acknowledgement is invalid.");
        }
        catch(StorageLifecycleConflictException){throw;}
        catch(RedisServerException){throw;} // Scripts perform only a read, then one native mutation, then a constant reply.
        catch(Exception cause){throw new RedisMutationReconciliationException(key,delete?"conditional DEL":"conditional SET",cause);}
    }

    public async Task DeleteAsync(string key,CancellationToken cancellationToken=default)
    {
        key=Key(key);cancellationToken.ThrowIfCancellationRequested();var db=await database();cancellationToken.ThrowIfCancellationRequested();
        try{await db.KeyDeleteAsync(key,CommandFlags.DemandMaster);} // False is an acknowledged already-absent key.
        catch(RedisServerException){throw;}
        catch(Exception cause){throw new RedisMutationReconciliationException(key,"DEL",cause);}
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken=default)
    {
        prefix=Key(prefix);cancellationToken.ThrowIfCancellationRequested();
        var db=await database();var servers=await primaries();
        if(servers.Count==0 || servers.Any(server=>server==null || server.IsReplica || !server.IsConnected))
            throw new InvalidOperationException("Complete connected Redis primary discovery is required for enumeration.");
        var pattern=new StringBuilder();
        foreach(var character in prefix){if(character is '\\' or '*' or '?' or '[' or ']')pattern.Append('\\');pattern.Append(character);}
        pattern.Append('*');
        var found=new HashSet<string>(StringComparer.Ordinal);
        foreach(var server in servers)
            await foreach(var nativeKey in server.KeysAsync(db.Database,pattern.ToString(),pageSize:256,flags:CommandFlags.DemandMaster).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key=Utf8.GetString((byte[])nativeKey!);
                if(!key.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidDataException("Redis enumeration returned an out-of-scope key.");
                found.Add(key); // SCAN enumerates keys, may repeat one during rehash; this is not a record snapshot.
            }
        return found.OrderBy(key=>key,StringComparer.Ordinal).ToArray();
    }

    public async Task VerifyAsync(CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();var db=await database();await db.PingAsync(CommandFlags.DemandMaster);
        _=await ListKeysAsync(keyPrefix,cancellationToken);
    }
}

internal sealed class RedisMutationReconciliationException(string key,string operation,Exception cause)
    : Exception("Redis mutation outcome is uncertain; reconcile the original key before replay.",cause),INativeMutationReconciliationRequired
{
    public string Key {get;}=key;
    public string Operation {get;}=operation;
    public string OriginalTransactionId=>string.Empty;
}
