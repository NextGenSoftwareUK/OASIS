using System.Text;
using System.Reflection;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace ProviderTestSupport;

// Shared by provider suites; no new runtime, test framework or accumulating per-run directories.
internal static class NativeCodeGenesisContract
{
    internal static void Verify(Func<string,string?,bool> generate,string? source,bool expected,Action<string>? inspect=null)
    {
        var root=Path.Combine(AppContext.BaseDirectory,"native-gen-fixture");
        if(Directory.Exists(root)||File.Exists(root))throw new InvalidOperationException("Existing source fixture must be inspected, not overwritten.");
        var directory=Path.Combine(root,"Solidity");var file=Path.Combine(directory,"Contract.sol");
        try{
            var result=generate(root,source);
            if(result!=expected)throw new InvalidOperationException($"NativeCodeGenesis reported {result}; expected {expected} for the supplied source.");
            if(expected){
                if(!File.Exists(file)||new FileInfo(file).Length==0||(source!=null&&!File.ReadAllBytes(file).SequenceEqual(new UTF8Encoding(false).GetBytes(source))))
                    throw new InvalidOperationException("Successful source generation must write the exact UTF-8 artifact.");
                inspect?.Invoke(file);
            }else if(Directory.Exists(root)||File.Exists(root))throw new InvalidOperationException("Rejected source generation must not create an output artifact or directory.");
        }finally{
            // Only these known files in this invocation's initially absent fixture; never recursive removal.
            if(File.Exists(file))File.Delete(file);
            if(Directory.Exists(directory))Directory.Delete(directory);
            if(Directory.Exists(root))Directory.Delete(root);
        }
    }

    internal static void VerifySchema(Func<ICelestialBody,string,bool> generate)
    {
        var holon=new Holon {Name="Seed",Nodes=new List<INode>{
            new Node {NodeName="Age",NodeType=NodeType.Int},new Node {NodeName="Alive",NodeType=NodeType.Bool},new Node {NodeName="Label",NodeType=NodeType.String}}};
        var zome=PropertyProxy.Make<IZome>("Children",new List<IHolon>{holon});
        var core=PropertyProxy.Make<ICelestialBodyCore>("Zomes",new List<IZome>{zome});
        var body=PropertyProxy.Make<ICelestialBody>("Name","Garden","CelestialBodyCore",core);
        Verify((path,_)=>generate(body,path),null,true,file=>{
            var root=new DirectoryInfo(AppContext.BaseDirectory);
            while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Docker/oasis-dependency-versions.env")))root=root.Parent;
            if(root==null)throw new InvalidOperationException("Retained checkout required.");
            var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
            start.ArgumentList.Add("-e");start.ArgumentList.Add("""
                const {createRequire}=require('node:module');const solc=createRequire(process.argv[1])('solc');
                if(solc.version()!=='0.8.26+commit.8a97fa7a.Emscripten.clang')throw Error('Reuse the retained pinned compiler');
                let input='';process.stdin.setEncoding('utf8');process.stdin.on('data',x=>input+=x);process.stdin.on('end',()=>{
                  const result=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources:{'Contract.sol':{content:input}},settings:{evmVersion:'paris',outputSelection:{'*':{'*':['evm.bytecode.object']}}}})));
                  const errors=(result.errors||[]).filter(x=>x.severity==='error');
                  if(errors.length){process.stderr.write(errors.map(x=>x.formattedMessage).join('\n'));process.exitCode=1;return;}
                  const bytecode=result.contracts['Contract.sol'].Garden.evm.bytecode.object;
                  if(!bytecode.length)throw Error('No compiled bytecode');process.stdout.write('compiled Garden with '+solc.version());
                });
                """);
            start.ArgumentList.Add(Path.Combine(root.FullName,"Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json"));
            using var child=Process.Start(start)??throw new InvalidOperationException("Retained compiler did not start.");
            var output=child.StandardOutput.ReadToEndAsync();var error=child.StandardError.ReadToEndAsync();
            child.StandardInput.Write(File.ReadAllText(file));child.StandardInput.Close();
            if(!child.WaitForExit(15000)){child.Kill(true);child.WaitForExit();throw new TimeoutException("Owned compiler exceeded15seconds.");}
            if(child.ExitCode!=0)throw new InvalidOperationException(error.GetAwaiter().GetResult());
            if(!output.GetAwaiter().GetResult().StartsWith("compiled Garden with ",StringComparison.Ordinal))throw new InvalidOperationException("Compiler evidence missing.");
        });
    }

    public class PropertyProxy:DispatchProxy
    {
        private Dictionary<string,object> values=new();
        internal static T Make<T>(params object[] properties)where T:class{
            var proxy=Create<T,PropertyProxy>();var state=(PropertyProxy)(object)proxy;
            for(var index=0;index<properties.Length;index+=2)state.values.Add((string)properties[index],properties[index+1]);return proxy;
        }
        protected override object? Invoke(MethodInfo? method,object?[]? args)
            =>method?.Name.StartsWith("get_",StringComparison.Ordinal)==true&&values.TryGetValue(method.Name[4..],out var value)?value:throw new InvalidOperationException("Unexpected fixture member: "+method?.Name);
    }
}
