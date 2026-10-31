using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class HolonWriteOwnershipTests
{
    [Theory]
    [InlineData("null","OWNERSHIP_LOOKUP_FAILED")]
    [InlineData("error","OWNERSHIP_LOOKUP_FAILED")]
    [InlineData("warning","OWNERSHIP_LOOKUP_FAILED")]
    [InlineData("wrong-id","OWNERSHIP_LOOKUP_FAILED")]
    [InlineData("unconfirmed-absence","OWNERSHIP_LOOKUP_FAILED")]
    [InlineData("other-owner","OWNERSHIP_FORBIDDEN")]
    [InlineData("empty-caller","OWNERSHIP_FORBIDDEN")]
    [InlineData("unowned","OWNERSHIP_FORBIDDEN")]
    [InlineData("owner",null)]
    [InlineData("absent",null)]
    public void AuthorizationFailsClosedUnlessLookupEstablishesPermission(string scenario,string? code)
    {
        var id=Guid.NewGuid();var caller=Guid.NewGuid();var cause=new IOException("owned lookup failure");
        var existing=new Holon {Id=id,CreatedByAvatarId=caller};
        var lookup=new OASISResult<IHolon> {Result=existing,IsLoaded=true};
        if(scenario=="error") {lookup.IsError=true;lookup.Exception=cause;}
        if(scenario=="warning") {lookup.IsWarning=true;lookup.Exception=cause;}
        if(scenario=="wrong-id") existing.Id=Guid.NewGuid();
        if(scenario=="other-owner") existing.CreatedByAvatarId=Guid.NewGuid();
        if(scenario=="unowned") existing.CreatedByAvatarId=Guid.Empty;
        if(scenario=="absent") lookup.Result=null;
        if(scenario=="unconfirmed-absence") {lookup.Result=null;lookup.IsLoaded=false;}
        var result=HolonWriteOwnership.Validate(scenario=="null" ? null!:lookup,id,scenario=="empty-caller" ? Guid.Empty:caller);
        result.ErrorCode.Should().Be(code);result.IsError.Should().Be(code!=null);result.Result.Should().Be(code==null);
        if(scenario is "error" or "warning") result.Exception.Should().BeSameAs(cause);
    }
}
