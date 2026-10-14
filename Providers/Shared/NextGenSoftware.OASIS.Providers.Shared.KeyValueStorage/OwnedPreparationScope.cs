using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Async-flow ownership whose closed state is also visible to inherited detached tasks.</summary>
public sealed class OwnedPreparationScope
{
    private readonly AsyncLocal<Owner> _current=new();
    private sealed class Owner(Func<string,string,PreparedSubmissionServer> factory)
    {
        private readonly object _gate=new();
        private bool _closed;
        public void Close(){lock(_gate){_closed=true;}}
        public PreparedSubmissionServer Create(string operation,string key)
        {
            lock(_gate){
                if(_closed)throw new InvalidOperationException("Native storage execution owner has already completed.");
                return factory(operation,key)??throw new InvalidOperationException("Native storage execution owner returned no preparation gate.");
            }
        }
    }
    public Task<T> RunAsync<T>(Func<Task<T>> operation,Func<PreparedSubmissionServer> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return RunAsync(operation,(_,_)=>factory());
    }
    public async Task<T> RunAsync<T>(Func<Task<T>> operation,Func<string,string,PreparedSubmissionServer> factory)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(factory);
        if(_current.Value!=null)throw new InvalidOperationException("Native storage execution ownership cannot be nested.");
        var owner=new Owner(factory);_current.Value=owner;
        try{return await operation();}finally{owner.Close();_current.Value=null;}
    }
    public PreparedSubmissionServer CreateCurrent(string operation=null,string key=null)=>_current.Value?.Create(operation,key);
}
