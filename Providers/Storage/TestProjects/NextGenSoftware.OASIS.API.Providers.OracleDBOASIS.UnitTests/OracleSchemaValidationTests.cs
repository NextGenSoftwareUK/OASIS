using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleSchemaValidationTests
{
    [DataTestMethod]
    [DataRow("OASIS_AVATARS","USERNAME","C",true)][DataRow("OASIS_AVATARS","EMAIL","C",true)]
    [DataRow("OASIS_AVATAR_DETAILS","USERNAME","C",true)][DataRow("OASIS_AVATAR_DETAILS","EMAIL","C",true)]
    [DataRow("OASIS_AVATARS","USERNAME","B",false)][DataRow("OASIS_AVATARS","EMAIL",null,false)]
    [DataRow("OASIS_AVATAR_DETAILS","USERNAME","B",false)][DataRow("OASIS_AVATAR_DETAILS","EMAIL","B",false)]
    [DataRow("OASIS_WRITE_RECEIPTS","REQUEST_HASH","B",true)][DataRow("OASIS_HOLONS","DATA_JSON",null,true)]
    public void IdentityColumnsCannotAdvertiseCharactersWithByteSemantics(string table,string column,string? semantics,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateIdentityCharacterSemantics",BindingFlags.NonPublic|BindingFlags.Static)!;
        var args=new object?[]{table,column,semantics};
        if(accepted)method.Invoke(null,args);
        else Assert.IsInstanceOfType<InvalidDataException>(Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException);
    }

    [DataTestMethod]
    [DataRow("CREATED_DATE","TIMESTAMP",null,false)]
    [DataRow("MODIFIED_DATE","TIMESTAMP",0,false)]
    [DataRow("COMMITTED_AT","TIMESTAMP",null,false)]
    [DataRow("CREATED_DATE","TIMESTAMP",12,true)]
    [DataRow("MODIFIED_DATE","TIMESTAMP",12,true)]
    [DataRow("COMMITTED_AT","TIMESTAMP",12,true)]
    [DataRow("DATA_JSON","CLOB",null,true)]
    [DataRow("ID","RAW",null,true)]
    public void OmittedNativeAuditColumnsRequireAnInsertDefault(string column,string required,int? length,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateStorageInsertDefault",BindingFlags.NonPublic|BindingFlags.Static)!;
        var args=new object?[]{"OASIS_TEST",column,required,length};
        if(accepted)method.Invoke(null,args);
        else Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException,typeof(InvalidDataException));
    }
    [DataTestMethod][DataRow("Y",true)][DataRow("N",false)]
    public void ReceiptResultAllowsItsExistingTransactionalPlaceholder(string nullable,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateStorageColumnProperties",BindingFlags.NonPublic|BindingFlags.Static)!;
        var args=new object?[]{"OASIS_WRITE_RECEIPTS","RESULT_JSON","CLOB",nullable,null,null,0};
        if(accepted)method.Invoke(null,args);
        else Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException,typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("DATA_JSON","CLOB","N",null,null,0,true)]
    [DataRow("DATA_JSON","CLOB","Y",null,null,0,false)]
    [DataRow("PARENT_HOLON_ID","RAW","Y",null,null,0,true)]
    [DataRow("PARENT_HOLON_ID","RAW","N",null,null,0,false)]
    [DataRow("IS_DELETED","NUMBER","N",1,0,0,true)]
    [DataRow("IS_DELETED","NUMBER","N",1,1,0,false)]
    [DataRow("IS_DELETED","NUMBER","N",2,-1,0,false)]
    [DataRow("IS_DELETED","NUMBER","N",null,null,0,true)]
    [DataRow("HOLON_TYPE","NUMBER","N",10,0,0,true)]
    [DataRow("HOLON_TYPE","NUMBER","N",9,0,0,false)]
    [DataRow("HOLON_TYPE","NUMBER","N",12,2,0,true)]
    [DataRow("HOLON_TYPE","NUMBER","N",10,2,0,false)]
    [DataRow("HOLON_TYPE","NUMBER","N",12,-1,0,false)]
    [DataRow("USERNAME","VARCHAR2","N",null,null,256,true)]
    [DataRow("USERNAME","VARCHAR2","N",null,null,64,false)]
    [DataRow("REQUEST_HASH","VARCHAR2","N",null,null,64,true)]
    [DataRow("REQUEST_HASH","VARCHAR2","N",null,null,63,false)]
    public void ColumnPropertiesMustPreserveRequiredNullsIntegersAndText(string column,string required,string nullable,int? precision,int? scale,int characters,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateStorageColumnProperties",BindingFlags.NonPublic|BindingFlags.Static)!;
        var args=new object?[]{"OASIS_TEST",column,required,nullable,precision,scale,characters};
        if(accepted)method.Invoke(null,args);
        else Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException,typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("ID","ID","ENABLED","VALIDATED","NOT DEFERRABLE","VALID",true)]
    [DataRow("OPERATION_ID","OPERATION_ID","ENABLED","VALIDATED","NOT DEFERRABLE","VALID",true)]
    [DataRow("ID","","ENABLED","VALIDATED","NOT DEFERRABLE","VALID",false)]
    [DataRow("ID","ID,VERSION","ENABLED","VALIDATED","NOT DEFERRABLE","VALID",false)]
    [DataRow("OPERATION_ID","REQUEST_HASH","ENABLED","VALIDATED","NOT DEFERRABLE","VALID",false)]
    [DataRow("ID","ID","DISABLED","VALIDATED","NOT DEFERRABLE","VALID",false)]
    [DataRow("ID","ID","ENABLED","NOT VALIDATED","NOT DEFERRABLE","VALID",false)]
    [DataRow("ID","ID","ENABLED","VALIDATED","DEFERRABLE","VALID",false)]
    [DataRow("ID","ID","ENABLED","VALIDATED","NOT DEFERRABLE","UNUSABLE",false)]
    [DataRow("ID","ID","ENABLED","VALIDATED","NOT DEFERRABLE",null,false)]
    public void IdentityAndReceiptPrimaryKeysMustEnforceNativeUniqueness(string key,string names,string status,string validated,string deferrable,string? indexStatus,bool accepted)
    {
        var method=typeof(OracleDBOASIS).GetMethod("ValidateStoragePrimaryKey",BindingFlags.NonPublic|BindingFlags.Static)!;
        var columns=names.Length==0?Array.Empty<string>():names.Split(',');
        var args=new object?[]{"OASIS_TEST",key,columns,status,validated,deferrable,indexStatus};
        if(accepted)method.Invoke(null,args);
        else Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,args)).InnerException,typeof(InvalidDataException));
    }

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
