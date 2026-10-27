#nullable enable
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Fail-closed checkpoint publication. Preserve the first failure and its pending bytes.
/// A suite's error-reporting callback must not mask a publication error by creating .next again.</summary>
internal sealed class AtomicConformanceCheckpoint<T>(string path)
{
    private ExceptionDispatchInfo? failure;
    public async Task WriteAsync(T report)
    {
        failure?.Throw();
        try {
            await using(var file=new FileStream(path+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(file,report);file.Flush(true);
            }
            File.Move(path+".next",path,overwrite:true);
        }
        catch(Exception cause){failure=ExceptionDispatchInfo.Capture(cause);throw;}
    }
}
