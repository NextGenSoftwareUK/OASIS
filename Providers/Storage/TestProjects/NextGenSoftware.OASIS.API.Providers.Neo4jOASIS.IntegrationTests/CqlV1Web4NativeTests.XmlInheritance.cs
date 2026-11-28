using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlInheritanceUsesPolymorphicProviderMappings(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:(provider,owned,timings)=>VerifyMappedInheritanceJourney(provider,owned,timings,TphXmlBuilder),
            nativeJourneySuffix:"-xml-inheritance-native.json");

    private static CosmicModelBuilder TphXmlBuilder()
    {
        const string xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
              <class name='Animal' discriminator-value='animal'>
                <id name='Id' type='Guid' generator='assigned'/>
                <discriminator column='tph_kind' type='string'/>
                <property name='Sound' column='tph_sound'/>
                <subclass name='Dog' discriminator-value='dog'>
                  <property name='Breed' column='tph_breed'/>
                  <subclass name='Puppy' discriminator-value='puppy'>
                    <property name='AgeWeeks' column='tph_age_weeks' type='integer'/>
                  </subclass>
                </subclass>
                <subclass name='Cat' discriminator-value='cat'>
                  <property name='Indoor' column='tph_indoor' type='boolean'/>
                </subclass>
              </class>
            </hibernate-mapping>
            """;
        var contracts=new Dictionary<string,Type>{{"Animal",typeof(TphAnimalHolon)},{"Dog",typeof(TphDogHolon)},
            {"Puppy",typeof(TphPuppyHolon)},{"Cat",typeof(TphCatHolon)}};
        var builder=new CosmicModelBuilder();
        // Failed imports publish no partial class/property/hierarchy state.
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("discriminator-value='cat'","discriminator-value='dog'"),contracts).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("name='Puppy'","name='Unlisted'"),contracts).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("<discriminator column='tph_kind' type='string'/>",""),contracts).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("column='tph_kind' type='string'","column='tph_sound' type='string'"),contracts).IsError);
        var imported=builder.ImportNHibernateXml(xml,contracts);
        Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message+ built.Exception);
        Assert.IsTrue(built.Result.TryGetDiscriminator(typeof(TphPuppyHolon),out var key,out var value));
        Assert.AreEqual("tph_kind",key);Assert.AreEqual("puppy",value);
        return builder;
    }
}
