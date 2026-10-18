using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidRdfParsingTests
{
    private const string Id="09d3be1f-bdd9-4b61-bccf-6377a9f92350";
    private static object Parse(string method,string turtle)
    {
        using var provider=new SOLIDOASIS("http://127.0.0.1:1");
        try{return typeof(SOLIDOASIS).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(provider,new object[]{turtle})!;}
        catch(TargetInvocationException error)when(error.InnerException!=null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
    }
    [Fact]
    public void PrefixNamesAreNotPredicateIdentity()
    {
        var record=(IHolon)Parse("ParseRDFToHolon",$"@prefix x: <https://oasisomniverse.one/ns/> . @prefix n: <http://www.w3.org/2000/01/rdf-schema#> . <https://pod.invalid/a> x:id \"{Id}\"; n:label \"oak\" .");
        Assert.Equal(Guid.Parse(Id),record.Id);Assert.Equal("oak",record.Name);
    }
    [Fact]
    public void EscapedLiteralIsDecodedWithoutTruncation()
    {
        var record=(IHolon)Parse("ParseRDFToHolon",$"@prefix oasis: <https://oasisomniverse.one/ns/> . @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> . <https://pod.invalid/a> oasis:id \"{Id}\"; rdfs:label \"oak \\\"tree\\\"\\nseed\" .");
        Assert.Equal("oak \"tree\"\nseed",record.Name);
    }
    [Fact]
    public void MultipleSubjectsRemainDistinctRecords()
    {
        var second=Guid.NewGuid();
        var records=((IEnumerable<IHolon>)Parse("ParseRDFToHolons",$"@prefix oasis: <https://oasisomniverse.one/ns/> . @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> . <https://pod.invalid/a> oasis:id \"{Id}\"; rdfs:label \"one\" . <https://pod.invalid/b> oasis:id \"{second}\"; rdfs:label \"two\" .")).ToList();
        Assert.Equal(2,records.Count);Assert.Contains(records,x=>x.Id==Guid.Parse(Id)&&x.Name=="one");Assert.Contains(records,x=>x.Id==second&&x.Name=="two");
    }
    [Theory]
    [InlineData("not RDF")]
    [InlineData("<https://pod.invalid/a> <https://oasisomniverse.one/ns/id> \"not-an-id\" .")]
    [InlineData("<https://pod.invalid/a> <https://oasisomniverse.one/ns/id> \"00000000-0000-0000-0000-000000000000\" .")]
    [InlineData("<https://pod.invalid/a> <https://other.invalid/id> \"ignored\" .")]
    [InlineData("@prefix o: <https://oasisomniverse.one/ns/> . <https://pod.invalid/a> o:id \"09d3be1f-bdd9-4b61-bccf-6377a9f92350\" . <https://pod.invalid/b> o:id \"09d3be1f-bdd9-4b61-bccf-6377a9f92350\" .")]
    [InlineData("@prefix o: <https://oasisomniverse.one/ns/> . <https://pod.invalid/a> o:id \"09d3be1f-bdd9-4b61-bccf-6377a9f92350\"; <http://www.w3.org/2000/01/rdf-schema#label> \"one\", \"two\" .")]
    public void InvalidSingleRecordCannotBecomeEmptyIdentitySuccess(string turtle)
        =>Assert.ThrowsAny<Exception>(()=>Parse("ParseRDFToHolon",turtle));
}
