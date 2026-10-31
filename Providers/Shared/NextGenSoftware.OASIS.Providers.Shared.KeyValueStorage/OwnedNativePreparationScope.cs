using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>In-process SDK preparation ownership. Persists before dispatch; never admits, retries or settles.</summary>
public sealed class OwnedNativePreparationScope
{
    private readonly AsyncLocal<Owner> _current=new();
    internal sealed class Owner(Func<JsonElement,CancellationToken,Task> persist,CancellationToken token)
    {
        private readonly object _gate=new();
        private bool _closed;
        private void CheckLocked(){if(_closed)throw new InvalidOperationException("Native preparation owner has completed.");token.ThrowIfCancellationRequested();}
        public CancellationToken Token {get {lock(_gate){CheckLocked();return token;}}}
        public void Close(){lock(_gate){_closed=true;}}
        public async Task PersistAsync(JsonElement evidence)
        {
            lock(_gate){CheckLocked();}
            await persist(evidence.Clone(),token);
            lock(_gate){CheckLocked();}
        }
        public Task<T> Submit<T>(Func<Task<T>> submit)
        {
            // Linearize starting the native invocation against closing this owner.
            lock(_gate){CheckLocked();return submit()??throw new InvalidOperationException("Native submission returned no task.");}
        }
    }
    public sealed class PreparedInvocation
    {
        private readonly Owner _owner;
        private readonly OwnedNativePreparationScope _scope;
        private int _used;
        internal PreparedInvocation(OwnedNativePreparationScope scope,Owner owner){_scope=scope;_owner=owner;}
        public Task<T> SubmitAsync<T>(Func<Task<T>> submit)
        {
            ArgumentNullException.ThrowIfNull(submit);
            if(!ReferenceEquals(_scope._current.Value,_owner))throw new InvalidOperationException("Prepared native invocation cannot move between execution owners.");
            if(Interlocked.CompareExchange(ref _used,1,0)!=0)throw new InvalidOperationException("Prepared native invocation is single-use; reconcile instead of replaying.");
            if(_owner==null)return submit()??throw new InvalidOperationException("Native submission returned no task.");
            return _owner.Submit(submit);
        }
    }
    public bool HasCurrent=>_current.Value!=null;
    public CancellationToken CurrentToken=>_current.Value?.Token??default;
    public async Task<T> RunAsync<T>(Func<Task<T>> operation,Func<JsonElement,CancellationToken,Task> persist,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(persist);token.ThrowIfCancellationRequested();
        if(_current.Value!=null)throw new InvalidOperationException("Native preparation ownership cannot be nested.");
        var owner=new Owner(persist,token);_current.Value=owner;
        try{return await operation();}finally{owner.Close();_current.Value=null;}
    }
    public Task<PreparedInvocation> PrepareCurrentAsync(JsonElement evidence)=>PrepareCurrentAsync(()=>evidence);
    public Task<PreparedInvocation> PrepareCurrentAsync(Func<JsonElement> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var owner=_current.Value;
        if(owner==null)return Task.FromResult(new PreparedInvocation(this,null));
        return PrepareOwnedAsync(owner,evidence);
    }
    private async Task<PreparedInvocation> PrepareOwnedAsync(Owner owner,Func<JsonElement> evidence)
    {
        owner.Token.ThrowIfCancellationRequested();
        await owner.PersistAsync(evidence());
        return new PreparedInvocation(this,owner);
    }
}
