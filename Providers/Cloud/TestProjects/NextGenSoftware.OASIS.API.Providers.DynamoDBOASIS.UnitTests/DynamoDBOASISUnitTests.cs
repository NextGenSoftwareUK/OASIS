using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests
{
    [TestClass]
    public class DynamoDBOASISProviderTests
    {
        [TestMethod]
        public void AwsRequestCorrelationIsNotAnOriginalTransactionReceipt()
        {
            var sdk=new Amazon.DynamoDBv2.AmazonDynamoDBException("owned SDK failure") {RequestId="owned-request-correlation"};
            var evidence=new DynamoDBMutationReconciliationException("OasisHolons","owned-key",sdk);
            Assert.AreSame(sdk,evidence.InnerException);
            Assert.AreEqual("owned-request-correlation",sdk.RequestId);
            Assert.AreEqual(string.Empty,evidence.OriginalTransactionId);
            Assert.AreEqual(sdk.RequestId,evidence.SdkRequestId);
            Assert.AreEqual(0,NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(evidence).Count);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
        }

        private sealed class PartialWriter : DynamoDBOASIS
        {
            public int Writes;
            public readonly Exception Cause=new DynamoDBMutationReconciliationException("OasisHolons","owned-second-key",new System.IO.IOException("owned lost reply"));
            public PartialWriter():base("owned-key","owned-secret","us-east-1") {}
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> SaveHolonAsync(
                NextGenSoftware.OASIS.API.Core.Interfaces.IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            {
                Writes++;
                return Task.FromResult(Writes==1 ? new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {Result=holon,IsSaved=true}
                    :new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {IsError=true,Exception=Cause,Message="owned uncertain write"});
            }
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task GraphPreflightRejectsInvalidSelectionWithoutPublishingIdsOrWriting(bool synchronous)
        {
            foreach(var invalid in new[] {"cycle","duplicate","parent","null"})
            {
                var provider=new PartialWriter();
                var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
                var child=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
                root.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {child};
                if(invalid=="cycle") child.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {root};
                if(invalid=="duplicate") root.Children.Add(child);
                if(invalid=="parent") child.ParentHolonId=Guid.NewGuid();
                if(invalid=="null") root.Children.Add(null!);
                var result=synchronous ? provider.SaveHolons(new[] {root}):await provider.SaveHolonsAsync(new[] {root});
                Assert.IsTrue(result.IsError,invalid);Assert.IsNotNull(result.Exception);
                Assert.AreEqual(0,provider.Writes,invalid);
                Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);
            }
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task GraphUncertaintyStopsDescendantsAndPublishesOnlyAcknowledgedRoot(bool synchronous)
        {
            var provider=new PartialWriter();
            var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            var child=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            var leaf=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            root.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {child};
            child.Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {leaf};
            var result=synchronous ? provider.SaveHolons(new[] {root}):await provider.SaveHolonsAsync(new[] {root});
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(2,provider.Writes);
            var partial=(DynamoDBPartialMutationException)result.Exception;
            Assert.AreSame(provider.Cause,partial.InnerException);
            Assert.AreNotEqual(Guid.Empty,root.Id);Assert.AreEqual(root.Id,partial.AcknowledgedRecordIds.Single());
            Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,child.ParentHolonId);Assert.AreEqual(Guid.Empty,leaf.Id);
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task ImportRetainsAcknowledgedIdentitiesAndStopsAfterUncertainty(bool synchronous)
        {
            var provider=new PartialWriter();
            var rows=Enumerable.Range(0,3).Select(_=>new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()}).ToArray();
            var result=synchronous ? provider.Import(rows):await provider.ImportAsync(rows);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);
            var partial=(DynamoDBPartialMutationException)result.Exception;
            Assert.AreSame(provider.Cause,partial.InnerException);
            Assert.AreEqual(rows[0].Id,partial.AcknowledgedRecordIds.Single());Assert.AreEqual(2,provider.Writes);
        }

        [TestMethod]
        public async Task BatchPreflightRejectsNullAndDuplicateBeforeAnyWrites()
        {
            var provider=new PartialWriter();var row=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
            Assert.IsTrue((await provider.SaveHolonsAsync(new[] {row,row})).IsError);
            Assert.IsTrue((await provider.SaveHolonsAsync(new NextGenSoftware.OASIS.API.Core.Interfaces.IHolon[] {row,null!})).IsError);
            Assert.AreEqual(0,provider.Writes);
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task QueryPaginationIncludesLaterPagesAndDoesNotHideLaterFailure(bool fail)
        {
            int calls=0;
            var cause=new System.IO.IOException("owned later page failure");
            Func<Amazon.DynamoDBv2.Model.QueryRequest,Task<Amazon.DynamoDBv2.Model.QueryResponse>> query=request=> {
                calls++;
                if(calls==2) {
                    Assert.AreEqual("cursor",request.ExclusiveStartKey["id"].S);
                    if(fail) throw cause;
                }
                return Task.FromResult(new Amazon.DynamoDBv2.Model.QueryResponse {
                    HttpStatusCode=System.Net.HttpStatusCode.OK,
                    Items=new() {new() {["id"]=new() {S="page-"+calls}}},
                    LastEvaluatedKey=calls==1 ? new() {["id"]=new() {S="cursor"}} : new()
                });
            };
            var task=(Task<List<Dictionary<string,Amazon.DynamoDBv2.Model.AttributeValue>>>)typeof(DynamoDBOASIS)
                .GetMethod("ReadQueryItemsAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null,new object[] {query,new Amazon.DynamoDBv2.Model.QueryRequest {TableName="OasisHolons"}})!;
            if(fail) Assert.AreSame(cause,await Assert.ThrowsExceptionAsync<System.IO.IOException>(()=>task));
            else {var rows=await task;Assert.AreEqual(2,rows.Count);Assert.AreEqual("page-2",rows[1]["id"].S);}
            Assert.AreEqual(2,calls);
        }

        [DataTestMethod]
        [DataRow("ACTIVE")]
        [DataRow("CREATING")]
        [DataRow("DELETING")]
        public async Task TableReadinessNeverClaimsSuccessAfterTimeoutOrTerminalState(string status)
        {
            int observations=0,delays=0;
            Func<Task<Amazon.DynamoDBv2.Model.DescribeTableResponse>> describe=()=> {
                observations++;
                return Task.FromResult(new Amazon.DynamoDBv2.Model.DescribeTableResponse {
                    Table=new Amazon.DynamoDBv2.Model.TableDescription {TableStatus=status}});
            };
            Func<Task> delay=()=> {delays++;return Task.CompletedTask;};
            var task=(Task)typeof(DynamoDBOASIS).GetMethod("WaitForTableActiveAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null,new object[] {describe,delay})!;
            if(status=="CREATING") {await Assert.ThrowsExceptionAsync<TimeoutException>(()=>task); Assert.AreEqual(30,observations); Assert.AreEqual(29,delays);}
            else if(status=="DELETING") {await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>task); Assert.AreEqual(1,observations); Assert.AreEqual(0,delays);}
            else {await task;Assert.AreEqual(1,observations);Assert.AreEqual(0,delays);}
        }

        private sealed class FailedIdentityProvider : DynamoDBOASIS
        {
            public readonly NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException Cause=new();
            public int Deletes;
            public FailedIdentityProvider():base("owned-key","owned-secret","us-east-1") { }
            private Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>> Failure()
                => Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar> {
                    IsError=true,Exception=Cause,ErrorCode=NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,Message="owned duplicate identity"});
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>> LoadAvatarByUsernameAsync(string u,int version=0)=>Failure();
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>> LoadAvatarByEmailAsync(string e,int version=0)=>Failure();
            public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> DeleteAvatarAsync(Guid id,bool softDelete=true) {
                Deletes++; throw new InvalidOperationException("Delete must not execute after lookup failure.");
            }
        }

        [DataTestMethod]
        [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
        public async Task SelectorDeletePreservesTerminalIdentityFailure(bool synchronous,bool email)
        {
            var provider=new FailedIdentityProvider();
            var result=synchronous ? (email ? provider.DeleteAvatarByEmail("owned"):provider.DeleteAvatarByUsername("owned"))
                : await (email ? provider.DeleteAvatarByEmailAsync("owned"):provider.DeleteAvatarByUsernameAsync("owned"));
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.Result); Assert.IsFalse(result.IsSaved);
            Assert.AreSame(provider.Cause,result.Exception); Assert.AreEqual("owned duplicate identity",result.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
            Assert.AreEqual(0,provider.Deletes);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task IdentityQueryReadsPastDeletedPageAndRejectsLaterDuplicate(bool duplicate)
        {
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.NewGuid(),Username="owned-user"};
            Dictionary<string,Amazon.DynamoDBv2.Model.AttributeValue> Row(bool deleted) => new() {
                ["id"]=new() {S=avatar.Id.ToString()}, ["is_deleted"]=new() {BOOL=deleted},
                ["data_json"]=new() {S=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(
                    new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=avatar.Id,Username=avatar.Username,
                        DeletedDate=deleted ? new DateTime(2026,10,16,0,0,0,DateTimeKind.Utc):DateTime.MinValue})}
            };
            int calls=0;
            Func<Amazon.DynamoDBv2.Model.ScanRequest,Task<Amazon.DynamoDBv2.Model.ScanResponse>> query=request=> {
                Assert.IsTrue(request.ConsistentRead);Assert.IsTrue(string.IsNullOrEmpty(request.IndexName));
                calls++;
                if(calls>1) Assert.AreEqual("next",request.ExclusiveStartKey["id"].S);
                return Task.FromResult(new Amazon.DynamoDBv2.Model.ScanResponse {
                    HttpStatusCode=System.Net.HttpStatusCode.OK,
                    Items=calls==1 ? new() {Row(true)} : duplicate ? new() {Row(false),Row(false)} : new() {Row(false)},
                    LastEvaluatedKey=calls==1 ? new() {["id"]=new() {S="next"}} : new()
                });
            };
            var task=(Task<NextGenSoftware.OASIS.API.Core.Holons.Avatar>)typeof(DynamoDBOASIS)
                .GetMethod("ReadUniqueAvatarAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null,new object[] {query,"username","owned-user",0})!;
            if(duplicate) await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException>(async()=>await task);
            else Assert.AreEqual(avatar.Id,(await task).Id);
            Assert.AreEqual(2,calls);
        }

        [TestMethod]
        public void StorageSerializerRetainsOwnedInventoryAndProgress()
        {
            var detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=Guid.NewGuid(),XP=321,Karma=29};
            var item=new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="owned seed"};
            detail.Inventory.Add(item);
            var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
            var json=(string)typeof(DynamoDBOASIS).GetMethod("Ser",flags)!.Invoke(null,new object[] {detail})!;
            var loaded=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail>(json);
            Assert.AreEqual(item.Id,loaded.Inventory.Single().Id);
            Assert.AreEqual(item.Name,loaded.Inventory.Single().Name);
            Assert.AreEqual(321,loaded.XP); Assert.AreEqual(29L,loaded.Karma);
            Assert.IsNotNull(loaded.Stats); Assert.IsNotNull(loaded.Skills);
        }

        private DynamoDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new DynamoDBOASIS("test-key", "test-secret", "us-east-1");
        }

        [TestMethod]
        public void ProviderType_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual(ProviderType.DynamoDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual("DynamoDBOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.ProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }

        [TestMethod]
        public void ProviderImplementsStorageInterface()
        {
            Assert.IsInstanceOfType(_provider, typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
