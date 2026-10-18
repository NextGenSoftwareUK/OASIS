using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSVersionTests
{
    private static Task<Holon> Read(int version,Func<string,Task<Holon>> fetch)
        => (Task<Holon>)typeof(IPFSOASIS).GetMethod("TraversePrivateVersionsAsync",BindingFlags.Static|BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Holon)).Invoke(null,new object[] {"current",version,fetch})!;

    [Theory]
    [InlineData(0,3)] [InlineData(3,3)] [InlineData(2,2)] [InlineData(1,1)] [InlineData(4,-1)]
    public async Task ExactVersionUsesRetainedChainWithoutInventingMissingVersions(int requested,int expected)
    {
        var id=Guid.NewGuid();
        var records=new Dictionary<string,Holon> {
            ["current"]=new() {Id=id,Version=3,PreviousVersionProviderUniqueStorageKey=new() {{ProviderType.IPFSOASIS,"middle"}}},
            ["middle"]=new() {Id=id,Version=2,PreviousVersionProviderUniqueStorageKey=new() {{ProviderType.IPFSOASIS,"old"}}},
            ["old"]=new() {Id=id,Version=1}};
        var result=await Read(requested,key=>Task.FromResult(records[key]));
        if(expected<0) Assert.Null(result); else Assert.Equal(expected,result.Version);
    }

    [Theory]
    [InlineData("cycle")] [InlineData("identity")] [InlineData("order")] [InlineData("empty-link")]
    public async Task InvalidChainNeverReturnsFabricatedHistory(string fault)
    {
        var current=new Holon {Id=Guid.NewGuid(),Version=3,PreviousVersionProviderUniqueStorageKey=new() {
            {ProviderType.IPFSOASIS,fault=="empty-link" ? " ":"previous"}}};
        var previous=new Holon {Id=fault=="identity" ? Guid.NewGuid():current.Id,Version=fault=="order" ? 4:2,
            PreviousVersionProviderUniqueStorageKey=new() {{ProviderType.IPFSOASIS,"current"}}};
        await Assert.ThrowsAsync<InvalidDataException>(()=>Read(1,key=>Task.FromResult(key=="current" ? current:previous)));
    }

    [Fact]
    public async Task SameVersionLifecycleLinkCanReachAnOlderVersion()
    {
        var id=Guid.NewGuid();var calls=0;
        var result=await Read(1,key=>Task.FromResult(new Holon {Id=id,Version=++calls<3 ? 2:1,
            PreviousVersionProviderUniqueStorageKey=new() {{ProviderType.IPFSOASIS,key+"-previous"}}}));
        Assert.Equal(1,result.Version);Assert.Equal(3,calls);
    }

    [Fact]
    public async Task NegativeVersionNeverReadsStorage()
    {
        var calls=0;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>Read(-1,_=> {calls++;return Task.FromResult(new Holon());}));
        Assert.Equal(0,calls);
    }

    [Fact]
    public async Task NativeReadCauseIsPreserved()
    {
        var cause=new IOException("owned unavailable retained CID");
        Assert.Same(cause,await Assert.ThrowsAsync<IOException>(()=>Read(1,_=>Task.FromException<Holon>(cause))));
    }
}
