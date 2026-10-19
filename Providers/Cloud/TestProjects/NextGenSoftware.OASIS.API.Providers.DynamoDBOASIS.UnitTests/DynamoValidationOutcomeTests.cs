using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoValidationOutcomeTests
{
    [DataTestMethod]
    [DataRow(400,"ValidationException",false)]
    [DataRow(500,"ValidationException",true)]
    [DataRow(400,"UnrecognizedError",true)]
    [DataRow(0,"ValidationException",true)]
    public async Task OnlyAuthoritativeValidationRejectionAvoidsUnknownMutationFence(int status,string code,bool uncertain)
    {
        var original=new AmazonDynamoDBException("owned invalid request",null,ErrorType.Sender,code,"owned-request",(HttpStatusCode)status);
        Func<Task<PutItemResponse>> mutation=()=>Task.FromException<PutItemResponse>(original);
        var method=typeof(DynamoDBOASIS).GetMethod("ObserveMutationAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(PutItemResponse));
        var pending=(Task<PutItemResponse>)method.Invoke(null,new object[]{mutation,"OasisHolons",Guid.NewGuid().ToString()})!;
        Exception? failure=null;try{await pending;}catch(Exception cause){failure=cause;}
        Assert.IsNotNull(failure);Assert.AreEqual(uncertain,NativeMutationReconciliation.IsRequired(failure));
        if(uncertain)Assert.AreSame(original,failure.InnerException);else Assert.AreSame(original,failure);
    }
}
