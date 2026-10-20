using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleSchemaValidationTests
{
    [DataTestMethod]
    [DataRow("ID","RAW","RAW",16,true)]
    [DataRow("ID","RAW","RAW",32,false)]
    [DataRow("ID","RAW","VARCHAR2",256,false)]
    [DataRow("USERNAME","VARCHAR2","VARCHAR2",255,false)]
    [DataRow("EMAIL","VARCHAR2","VARCHAR2",256,true)]
    [DataRow("REQUEST_HASH","VARCHAR2","VARCHAR2",64,true)]
    [DataRow("REQUEST_HASH","VARCHAR2","VARCHAR2",63,false)]
    [DataRow("DATA_JSON","CLOB","BLOB",4000,false)]
    [DataRow("DATA_JSON","CLOB","CLOB",4000,true)]
    [DataRow("CREATED_DATE","TIMESTAMP","TIMESTAMP(6)",11,true)]
    [DataRow("CREATED_DATE","TIMESTAMP","TIMESTAMP(6) WITH TIME ZONE",13,false)]
    [DataRow("HOLON_TYPE","NUMBER","NUMBER",22,true)]
    public void ExistingColumnMustMatchNativeContract(string column,string required,string actual,int length,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateStorageColumn",BindingFlags.NonPublic|BindingFlags.Static)!;
        var args=new object[]{"OASIS_TEST",column,required,actual,length};
        if(accepted)method.Invoke(null,args);
        else{
            var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args));
            Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
        }
    }
}
