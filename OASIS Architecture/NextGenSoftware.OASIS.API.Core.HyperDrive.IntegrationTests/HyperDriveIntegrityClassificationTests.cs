using System.Reflection;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class HyperDriveIntegrityClassificationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void StoredIntegrityFailuresAreTerminalButOrdinaryIoCanFailOver(int kind)
    {
        var invalid=new InvalidDataException("owned invalid persisted identity");
        Exception cause=kind switch {
            0=>invalid,
            1=>new InvalidOperationException("owned wrapper",invalid),
            2=>new AggregateException(new IOException("owned transport"),invalid),
            _=>new IOException("owned transport")};
        var result=new OASISResult<IHolon> {IsError=true,Exception=cause};
        var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=Guid.NewGuid()};
        var method=typeof(Managers.OASISHyperDrive.OASISHyperDrive).GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IHolon));
        Assert.Equal(kind==3,(bool)method.Invoke(null,new object[] {request,result})!);
    }
}
