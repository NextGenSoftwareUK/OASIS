using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class SqlitePreparationReadTests
{
    private static string OwnedPath => Path.Combine(AppContext.BaseDirectory,"owned-preparation-status.sqlite");

    [DataTestMethod][DataRow("stored")][DataRow("absent")][DataRow("foreign")][DataRow("corrupt")]
    public async Task FileLookupIsReadOnlyAndNeverPublishesForeignOrMalformedPreparation(string mode)
    {
        var path=Path.GetFullPath(OwnedPath);
        Assert.IsFalse(File.Exists(path),"Refuse to overwrite an existing unknown fixture.");
        var settings=new SqliteConnectionStringBuilder{DataSource=path,Pooling=false};
        var original=new HolonWritePreparation(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,
            new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:opaque")});
        try
        {
            using(var connection=new SqliteConnection(settings.ToString()))
            {
                connection.Open();using var schema=connection.CreateCommand();
                schema.CommandText="CREATE TABLE CosmicWritePreparation(OperationId TEXT PRIMARY KEY,ActorId TEXT,PayloadJson TEXT,CreatedAtUtc TEXT)";
                schema.ExecuteNonQuery();
                if(mode!="absent")
                {
                    using var insert=connection.CreateCommand();
                    insert.CommandText="INSERT INTO CosmicWritePreparation VALUES($op,$actor,$payload,$at)";
                    insert.Parameters.AddWithValue("$op",original.OperationId.ToString("D"));
                    insert.Parameters.AddWithValue("$actor",original.ActorId.ToString("D"));
                    insert.Parameters.AddWithValue("$payload",mode=="corrupt"?"invalid":ProtectedPreparationJson.Write(original));
                    insert.Parameters.AddWithValue("$at",DateTime.UtcNow.ToString("O"));insert.ExecuteNonQuery();
                }
            }
            var before=File.ReadAllBytes(path);
            using(var context=new DataContext(settings.ToString()))
            {
                var repository=new HolonRepository(context);
                var result=await repository.ReadHolonWritePreparationAsync(original.OperationId,mode=="foreign"?Guid.NewGuid():original.ActorId);
                Assert.AreEqual(mode is "foreign" or "corrupt",result.IsError,result.Message+result.Exception);
                Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
                if(mode=="stored")Assert.AreEqual(original.Records[0].Envelope,result.Result.Records[0].Envelope);
                else Assert.IsNull(result.Result);
                if(mode=="foreign")Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",result.ErrorCode);
                Assert.AreEqual(0,context.ChangeTracker.Entries().Count());
            }
            CollectionAssert.AreEqual(before,File.ReadAllBytes(path),"Lookup must leave all database bytes unchanged.");
        }
        finally
        {
            // This test creates only this fixed file, uses nonpooled handles,
            // and disposes all contexts/readers before removing its fixture.
            Assert.AreEqual(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar),Path.GetDirectoryName(path));
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task MissingFileIsAnErrorAndMustNotBeCreatedByRecovery()
    {
        Assert.IsFalse(File.Exists(OwnedPath));
        using var context=new DataContext(new SqliteConnectionStringBuilder{DataSource=OwnedPath,Pooling=false}.ToString());
        var result=await new HolonRepository(context).ReadHolonWritePreparationAsync(Guid.NewGuid(),Guid.NewGuid());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(File.Exists(OwnedPath));
    }
}
