using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

public partial class OracleObjectSdkTests
{
    [DataTestMethod]
    [DataRow("bytes")][DataRow("line-feed")][DataRow("carriage-return")][DataRow("nul")]
    public void InvalidPhysicalPrefixRejectsBeforeUsingClient(string mode)
    {
        using var client=Client();
        var prefix=mode switch {
            "bytes"=>new string('é',512)+"/",
            "line-feed"=>"tenant\n/",
            "carriage-return"=>"tenant\r/",
            _=>"tenant\0/"};
        var cause=Assert.ThrowsException<ArgumentException>(()=>new OracleObjectStorageBackend(client,"ns","bucket",prefix));
        Assert.AreEqual("objectPrefix",cause.ParamName);
    }

    [DataTestMethod]
    [DataRow("get",false)][DataRow("get",true)]
    [DataRow("observed",false)][DataRow("observed",true)]
    [DataRow("put",false)][DataRow("put",true)]
    [DataRow("create",false)][DataRow("create",true)]
    [DataRow("replace",false)][DataRow("replace",true)]
    [DataRow("delete",false)][DataRow("delete",true)]
    [DataRow("conditional-delete",false)][DataRow("conditional-delete",true)]
    [DataRow("list",false)][DataRow("list",true)]
    public async Task InvalidPhysicalNameRejectsBeforeDisposedSdkAccess(string operation,bool forbiddenCharacter)
    {
        using var client=Client();
        var backend=new OracleObjectStorageBackend(client,"ns","bucket","tenant/");
        client.Dispose(); // SDK access is a failure sentinel, never a network request.
        var key=forbiddenCharacter?"record\nname":new string('é',509);
        Assert.IsTrue(forbiddenCharacter||Encoding.UTF8.GetByteCount("tenant/"+key)==1025);
        Task Call()=>operation switch {
            "get"=>backend.GetAsync(key),"observed"=>backend.ReadObservedAsync(key),
            "put"=>backend.PutAsync(key,"value"),"create"=>backend.CreateOnlyAsync(key,"value"),
            "replace"=>backend.ReplaceUnchangedAsync(key,"value","revision"),
            "list"=>backend.ListKeysAsync(key),
            "delete"=>backend.DeleteAsync(key),_=>backend.DeleteUnchangedAsync(key,"revision")};
        var cause=await Assert.ThrowsExceptionAsync<ArgumentException>(Call);
        Assert.AreEqual(operation=="list"?"prefix":"key",cause.ParamName,"Reject the caller's physical-name input, not a later SDK failure.");
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ExactPhysicalUtf8LimitRemainsAValidSignedSdkWrite(bool supplementary)
    {
        var key=supplementary?string.Concat(Enumerable.Repeat("🌳",254))+"a":new string('é',508)+"a";
        Assert.AreEqual(1024,Encoding.UTF8.GetByteCount("tenant/"+key));
        // HTTP.sys rejects these long encoded path segments before HttpListener
        // delivers them. Inspect the real signed request on the SAME fixture port
        // without changing machine-wide HTTP.sys limits or adding a runtime.
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var listener=new TcpListener(IPAddress.Loopback,18661);
        listener.Start();
        using var client=Client();
        var server=Task.Run(async()=>{
            using var socket=await listener.AcceptTcpClientAsync(deadline.Token);
            using var stream=socket.GetStream();
            using var reader=new StreamReader(stream,Encoding.ASCII,false,4096,leaveOpen:true);
            var request=(await reader.ReadLineAsync(deadline.Token))!.Split(' ');
            Assert.AreEqual("PUT",request[0]);
            Assert.AreEqual("HTTP/1.1",request[2]);
            Assert.AreEqual("tenant/"+key,Uri.UnescapeDataString(request[1].Split("/o/",2)[1]));
            var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            for(var index=0;index<128;index++){
                var line=await reader.ReadLineAsync(deadline.Token);
                if(line=="")break;
                Assert.IsNotNull(line);
                var parts=line!.Split(':',2);Assert.AreEqual(2,parts.Length);
                headers.Add(parts[0],parts[1].Trim());
                Assert.IsTrue(index<127,"Bounded SDK header count required.");
            }
            StringAssert.StartsWith(headers["Authorization"],"Signature ");
            Assert.AreEqual("*",headers["If-None-Match"]);
            Assert.AreEqual("14",headers["Content-Length"]);
            var body=new char[14];var offset=0;
            while(offset<body.Length){var read=await reader.ReadAsync(body.AsMemory(offset),deadline.Token);Assert.IsTrue(read>0);offset+=read;}
            Assert.AreEqual("boundary-value",new string(body));
            var response=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nETag: \"boundary-revision\"\r\nopc-content-md5: "+headers["Content-MD5"]+"\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(response,deadline.Token);
        });
        try{
            var backend=new OracleObjectStorageBackend(client,"ns","bucket","tenant/");
            await Task.WhenAll(backend.CreateOnlyAsync(key,"boundary-value",deadline.Token),server);
        }finally{listener.Stop();}
    }
}
