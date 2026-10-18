using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HyperDriveJwtValidationTests
{
    private const string Secret="public-test-only-signing-key-0123456789012345678901234567890123456789";
    private const string Avatar="11111111-1111-1111-1111-111111111111";
    private static string Token(string mode)
    {
        var expires=mode=="expired"?DateTime.UtcNow.AddMinutes(-1):DateTime.UtcNow.AddMinutes(5);
        var claims=new List<Claim> {new Claim("sub",Avatar),new Claim("id",mode=="conflicting-id"?Guid.Empty.ToString():Avatar)};
        if(mode=="missing-id")claims.RemoveAll(claim=>claim.Type=="id");
        if(mode=="duplicate-sub")claims.Add(new Claim("sub",Guid.Empty.ToString()));
        if(mode=="conflicting-alias")claims.Add(new Claim(ClaimTypes.NameIdentifier,Guid.Empty.ToString()));
        if(mode=="duplicate-id")claims.Add(new Claim("id",Avatar));
        var key=new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mode=="wrong-signature"?new string('x',80):Secret));
        var token=new JwtSecurityToken(mode=="wrong-issuer"?"foreign":"OASIS",mode=="wrong-audience"?"foreign":"OASIS",claims,
            mode=="future-start"?DateTime.UtcNow.AddMinutes(1):DateTime.UtcNow.AddMinutes(-5),expires,new SigningCredentials(key,mode=="wrong-algorithm"?SecurityAlgorithms.HmacSha384:SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void ActualSignedJwtValidatesExactAvatarClaims()
    {
        var principal=OasisJwtValidation.Validate(Token("valid"),Secret,"OASIS");
        Assert.True(principal.Identity!.IsAuthenticated);Assert.Equal(Avatar,principal.FindFirst("sub")!.Value);
        Assert.Equal(Avatar,principal.FindFirst("id")!.Value);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("wrong-signature")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-algorithm")]
    [InlineData("conflicting-id")]
    [InlineData("missing-id")]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-sub")]
    [InlineData("conflicting-alias")]
    [InlineData("future-start")]
    public void InvalidCryptographicOrIdentityClaimsCannotAuthenticate(string mode)
    {
        // The installed JWT parser rejects array-valued reserved sub claims
        // before semantic validation; middleware explicitly handles this type.
        if(mode=="duplicate-sub")
            Assert.Throws<ArgumentException>(()=>OasisJwtValidation.Validate(Token(mode),Secret,"OASIS"));
        else
            Assert.ThrowsAny<SecurityTokenException>(()=>OasisJwtValidation.Validate(Token(mode),Secret,"OASIS"));
    }
}
