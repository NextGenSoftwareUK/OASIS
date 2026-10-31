using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

// In-memory control for shared provider policy; NOT Redis/native transport proof.
internal sealed class RedisControlledStore:IConditionalKeyValueBackend
{
    public readonly Dictionary<string,string> Items=new(StringComparer.Ordinal);
    public int Reads;
    public Task<string> GetAsync(string key,CancellationToken token=default){token.ThrowIfCancellationRequested();Reads++;return Task.FromResult(Items.GetValueOrDefault(key)!);}
    public Task<KeyValueObservation> ReadObservedAsync(string key,CancellationToken token=default)
    {token.ThrowIfCancellationRequested();Reads++;return Task.FromResult(Items.TryGetValue(key,out var value)?new KeyValueObservation(value,value):null!);}
    public Task PutAsync(string key,string value,CancellationToken token=default){token.ThrowIfCancellationRequested();Items[key]=value;return Task.CompletedTask;}
    public Task DeleteAsync(string key,CancellationToken token=default){token.ThrowIfCancellationRequested();Items.Remove(key);return Task.CompletedTask;}
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)
    {token.ThrowIfCancellationRequested();Reads++;return Task.FromResult<IReadOnlyList<string>>(Items.Keys.Where(key=>key.StartsWith(prefix,StringComparison.Ordinal)).ToArray());}
    public Task VerifyAsync(CancellationToken token=default){token.ThrowIfCancellationRequested();return Task.CompletedTask;}
    public Task CreateOnlyAsync(string key,string value,CancellationToken token=default)
    {token.ThrowIfCancellationRequested();if(!Items.TryAdd(key,value))throw new StorageLifecycleConflictException("Controlled create conflict.");return Task.CompletedTask;}
    public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken token=default)
    {token.ThrowIfCancellationRequested();Require(key,revision);Items[key]=value;return Task.CompletedTask;}
    public Task DeleteUnchangedAsync(string key,string revision,CancellationToken token=default)
    {token.ThrowIfCancellationRequested();Require(key,revision);Items.Remove(key);return Task.CompletedTask;}
    private void Require(string key,string revision){if(!Items.TryGetValue(key,out var value)||value!=revision)throw new StorageLifecycleConflictException("Controlled stale observation.");}
}
