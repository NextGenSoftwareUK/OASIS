using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleCommandBindingTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public void CanonicalWriteFactoryBindsRealSdkParametersWithoutOpeningConnection(int kind,bool creating)
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new NextGenSoftware.OASIS.API.Core.Holons.Avatar():kind==1?new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail():new NextGenSoftware.OASIS.API.Core.Holons.Holon();
        row.Id=Guid.NewGuid();var table=kind==0?"OASIS_AVATARS":kind==1?"OASIS_AVATAR_DETAILS":"OASIS_HOLONS";
        var json=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(row);using var connection=new OracleConnection();
        var factory=typeof(OracleDBOASIS).GetMethod("CreateCanonicalWriteCommand",BindingFlags.Static|BindingFlags.NonPublic)!;
        using var command=(OracleCommand)factory.Invoke(null,new object?[]{row,json,table,creating,connection,null})!;
        Assert.IsTrue(command.BindByName);Assert.AreSame(connection,command.Connection);StringAssert.StartsWith(command.CommandText,creating?"INSERT INTO ":"UPDATE ");
        CollectionAssert.AreEqual(row.Id.ToByteArray(),(byte[])command.Parameters["Id"].Value);Assert.AreEqual(json,command.Parameters["Json"].Value);Assert.AreEqual(OracleDbType.Clob,command.Parameters["Json"].OracleDbType);
        Assert.AreEqual(System.Data.ConnectionState.Closed,connection.State);
    }
    [DataTestMethod]
    [DataRow("SELECT :Id FROM DUAL WHERE :Id=:Id")]
    [DataRow("MERGE INTO OASIS_HOLONS tgt USING (SELECT :Id AS ID FROM DUAL) src ON (tgt.ID=src.ID) WHEN MATCHED THEN UPDATE SET tgt.DATA_JSON=:DataJson WHEN NOT MATCHED THEN INSERT(ID,DATA_JSON) VALUES(:Id,:DataJson)")]
    public void ProductionCommandsBindRepeatedParametersByName(string sql)
    {
        using var connection=new OracleConnection();
        var factory=typeof(OracleDBOASIS).GetMethod("CreateCommand",BindingFlags.Static|BindingFlags.NonPublic)!;
        using var command=(OracleCommand)factory.Invoke(null,new object[]{sql,connection})!;
        Assert.IsTrue(command.BindByName,"Named SQL must not depend on Oracle's positional/global default.");
        Assert.AreSame(connection,command.Connection);Assert.AreEqual(sql,command.CommandText);
        command.Parameters.Add("DataJson",OracleDbType.Clob).Value="owned payload";
        command.Parameters.Add("Id",OracleDbType.Raw).Value=Guid.NewGuid().ToByteArray();
        Assert.AreEqual(2,command.Parameters.Count);Assert.AreEqual("DataJson",command.Parameters[0].ParameterName);
        Assert.AreEqual(OracleDbType.Raw,command.Parameters["Id"].OracleDbType);
        Assert.AreEqual(System.Data.ConnectionState.Closed,connection.State,"SDK boundary test must not connect or execute SQL.");
    }
}
