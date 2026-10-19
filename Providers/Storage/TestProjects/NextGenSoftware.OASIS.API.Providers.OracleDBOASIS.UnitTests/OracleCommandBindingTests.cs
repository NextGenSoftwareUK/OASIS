using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleCommandBindingTests
{
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
