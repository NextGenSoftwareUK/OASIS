using System.Text;

namespace ProviderTestSupport;

// Shared by provider suites; no new runtime, test framework or accumulating per-run directories.
internal static class NativeCodeGenesisContract
{
    internal static void Verify(Func<string,string?,bool> generate,string? source,bool expected)
    {
        var root=Path.Combine(AppContext.BaseDirectory,"native-gen-fixture");
        if(Directory.Exists(root)||File.Exists(root))throw new InvalidOperationException("Existing source fixture must be inspected, not overwritten.");
        var directory=Path.Combine(root,"Solidity");var file=Path.Combine(directory,"Contract.sol");
        try{
            var result=generate(root,source);
            if(result!=expected)throw new InvalidOperationException($"NativeCodeGenesis reported {result}; expected {expected} for the supplied source.");
            if(expected){
                if(!File.Exists(file)||!File.ReadAllBytes(file).SequenceEqual(new UTF8Encoding(false).GetBytes(source!)))
                    throw new InvalidOperationException("Successful source generation must write the exact UTF-8 artifact.");
            }else if(Directory.Exists(root)||File.Exists(root))throw new InvalidOperationException("Rejected source generation must not create an output artifact or directory.");
        }finally{
            // Only these known files in this invocation's initially absent fixture; never recursive removal.
            if(File.Exists(file))File.Delete(file);
            if(Directory.Exists(directory))Directory.Delete(directory);
            if(Directory.Exists(root))Directory.Delete(root);
        }
    }
}
