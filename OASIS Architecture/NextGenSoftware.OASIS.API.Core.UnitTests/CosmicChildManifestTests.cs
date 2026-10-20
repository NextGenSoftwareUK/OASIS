using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChildManifestTests
{
    [Fact]
    public void ConditionalEnvelopeRetainsValidatedDetachedOriginalRoles()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        var manifest=new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Typed",new[]{child.Id})});
        var input=new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,manifest}};
        var write=new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,new[]{
                new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(root.Id,null),
                new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(child.Id,null)});
        var request=new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(write,
            NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,new[]{root},true,true,1,input);
        input.Clear();
        request.SubmittedRelationships[root.Id].Should().BeSameAs(manifest);
        Action mutate=()=>((IDictionary<Guid,HolonChildRoleManifest>)request.SubmittedRelationships).Clear();
        mutate.Should().Throw<NotSupportedException>();
        var invalid=new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Typed",new[]{Guid.NewGuid()})})}};
        Action forge=()=>new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(write,
            NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,new[]{root},true,true,1,invalid);
        forge.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("valid")] [InlineData("parent")] [InlineData("member")] [InlineData("edge")]
    public void RuntimeAdmissionBindsManifestToTheSelectedStagedGraph(string fault)
    {
        var source=new CosmicChildCollectionTests.TypedAggregate();source.Add(new Holon());
        using var plan=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[]{source},true,true,1);
        var root=plan.Roots.Single();
        var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest { Operation="SaveHolon",Payload=root,
            MaxChildDepth=1,SubmittedRelationships=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.SubmittedRelationships(plan.Roots) };
        if(fault=="edge")root.Children.Single().ParentHolonId=Guid.NewGuid();
        if(fault=="parent" || fault=="member")
            request.SubmittedRelationships=new Dictionary<Guid,HolonChildRoleManifest> {
                [root.Id]=new HolonChildRoleManifest(1,fault=="parent" ? Guid.NewGuid():root.Id,
                    new[]{new HolonChildRole("Typed",new[]{Guid.NewGuid()})}) };
        Action validate=()=>request.ValidateSubmittedRelationships();
        if(fault=="valid")validate.Should().NotThrow();else validate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RuntimeRequestRetainsOriginalRolesAfterStagingAndPlanDisposal()
    {
        var source=new CosmicChildCollectionTests.TypedAggregate();source.Add(new Holon());
        NextGenSoftware.OASIS.API.Core.StorageOperationRequest request;
        using(var plan=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[]{source},true,true,1))
        {
            var root=plan.Roots.Single();
            request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                Payload=root,SubmittedRelationships=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.SubmittedRelationships(plan.Roots) };
            var child=root.Children.Single();
            request.SubmittedRelationships[root.Id].Roles.Single(role=>role.Name=="Typed").ChildIds.Should().Equal(child.Id);
        }
        var copy=request.CopyWithPayload(request.Payload);
        copy.SubmittedRelationships.Should().BeSameAs(request.SubmittedRelationships);
        Action mutate=()=>((IDictionary<Guid,HolonChildRoleManifest>)copy.SubmittedRelationships).Clear();
        mutate.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedLazyInputIsRejectedWithoutEnumeratingBeyondAdmissionLimit(bool links)
    {
        var parent=Guid.NewGuid();
        var reads=0;
        IEnumerable<HolonChildRole> Roles()
        {
            while(true)
            {
                if(++reads>257)throw new InvalidOperationException("Unbounded role enumeration");
                yield return new HolonChildRole("role"+reads,Array.Empty<Guid>());
            }
        }
        IEnumerable<Guid> Links()
        {
            while(true)
            {
                if(++reads>65537)throw new InvalidOperationException("Unbounded link enumeration");
                yield return Guid.NewGuid();
            }
        }
        Action create=()=>{
            if(links)_=new HolonChildRole("Children",Links());
            else _=new HolonChildRoleManifest(1,parent,Roles());
        };
        create.Should().Throw<ArgumentException>();
        reads.Should().Be(links ? 65537:257);
    }

    [Fact]
    public void SavePlanManifestUsesAllocatedIdsAndFrozenSubmittedMembership()
    {
        var root=new CosmicChildCollectionTests.TypedAggregate();var child=new Holon();root.Add(child);
        using var plan=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[]{root},true,true,1);
        var staged=plan.Roots.Single();var stagedChild=staged.Children.Single();
        var manifest=plan.RelationshipManifests[staged.Id];
        manifest.ParentId.Should().Be(staged.Id);
        manifest.Roles.Single(role=>role.Name=="Typed").ChildIds.Should().Equal(stagedChild.Id);
        plan.RelationshipManifests.Should().NotContainKey(stagedChild.Id,"its child collections are excluded at depth one");
        root.Add(new Holon());
        manifest.Roles.Single(role=>role.Name=="Typed").ChildIds.Should().Equal(stagedChild.Id);
        root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();
    }

    [Fact]
    public void RootOnlySaveCannotEmitAnEmptyReplacementForUnobservedRelationships()
    {
        var root=new CosmicChildCollectionTests.TypedAggregate();root.Add(new Holon());
        using var plan=NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[]{root},false,true,0);
        plan.RelationshipManifests.Should().BeEmpty();
    }

    [Fact]
    public void WireRoundTripHasCanonicalBytesRegardlessOfRoleInputOrder()
    {
        var parent=Guid.NewGuid();var child=Guid.NewGuid();
        var roles=new[]{new HolonChildRole("Zomes",new[]{child}),new HolonChildRole("Children",Array.Empty<Guid>())};
        var json=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,parent,roles));
        HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,parent,roles.Reverse())).Should().Be(json);
        HolonChildRoleManifestJson.Serialize(HolonChildRoleManifestJson.Deserialize(json)).Should().Be(json);
    }

    [Theory]
    [InlineData("unknown")] [InlineData("duplicate")] [InlineData("missing")]
    [InlineData("version-type")] [InlineData("version-future")] [InlineData("identity-type")]
    [InlineData("identity-format")] [InlineData("trailing")]
    public void AmbiguousOrUnsupportedWireDataCannotBeHydrated(string fault)
    {
        var manifest=new HolonChildRoleManifest(1,Guid.NewGuid(),Array.Empty<HolonChildRole>());
        var json=HolonChildRoleManifestJson.Serialize(manifest);
        var document=Newtonsoft.Json.Linq.JObject.Parse(json);
        switch(fault)
        {
            case "unknown":document["restoreCallback"]="untrusted";break;
            case "missing":document.Remove("roles");break;
            case "version-type":document["schemaVersion"]="1";break;
            case "version-future":document["schemaVersion"]=2;break;
            case "identity-type":document["parentId"]=1;break;
            case "identity-format":document["parentId"]=manifest.ParentId.ToString("N");break;
        }
        json=document.ToString(Newtonsoft.Json.Formatting.None);
        if(fault=="duplicate")json=json.Replace("\"schemaVersion\":1","\"schemaVersion\":1,\"schemaVersion\":1");
        if(fault=="trailing")json+=" {}";
        Action read=()=>HolonChildRoleManifestJson.Deserialize(json);
        read.Should().Throw<System.IO.InvalidDataException>();
    }

    [Fact]
    public void CanonicalRoleOrderPreservesOrderedMembersAndDetachedInput()
    {
        var first=Guid.NewGuid();var second=Guid.NewGuid();var ids=new List<Guid>{second,first};
        var roles=new List<HolonChildRole>{new("Zomes",ids),new("Children",Array.Empty<Guid>())};
        var manifest=new HolonChildRoleManifest(1,Guid.NewGuid(),roles);
        ids.Clear();roles.Clear();
        manifest.Roles.Select(role=>role.Name).Should().Equal("Children","Zomes");
        manifest.Roles[1].ChildIds.Should().Equal(second,first);
        Action mutate=()=>((IList<Guid>)manifest.Roles[1].ChildIds).Clear();
        mutate.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void CanonicalSerializationRoundTripsDataWithoutLocalRestoreCallbacks()
    {
        var parent=Guid.NewGuid();var child=Guid.NewGuid();
        var source=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Zomes",new[]{child})});
        var json=StoredRecordJson.Serialize(source);
        var restored=StoredRecordJson.Deserialize<HolonChildRoleManifest>(json);
        restored.ParentId.Should().Be(parent);restored.SchemaVersion.Should().Be(1);
        restored.Roles.Single().ChildIds.Should().Equal(child);
        StoredRecordJson.Serialize(restored).Should().Be(json);
    }

    [Fact]
    public void ManifestUsesPlannedIdentitiesWithoutPublishingThemToNewChildren()
    {
        var root=new CosmicChildCollectionTests.TypedAggregate();var child=new Holon();root.Add(child);
        var planned=Guid.NewGuid();var parent=Guid.NewGuid();
        var manifest=HolonChildCollectionState.Capture(root).CreateManifest(parent,_=>planned);
        manifest.Roles.Single(role=>role.Name=="Typed").ChildIds.Should().Equal(planned);
        child.Id.Should().BeEmpty();root.Id.Should().BeEmpty();
        Action unseen=()=>HolonChildCollectionState.UnobservedEmpty.CreateManifest(parent,_=>planned);
        unseen.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("same-role")] [InlineData("different-roles")] [InlineData("self")]
    public void ManifestRejectsAmbiguousOwnership(string fault)
    {
        var parent=Guid.NewGuid();var child=fault=="self" ? parent:Guid.NewGuid();
        Action create=()=>new HolonChildRoleManifest(1,parent,fault=="same-role"
            ? new[]{new HolonChildRole("Children",new[]{child,child})}
            :fault=="different-roles" ? new[]{new HolonChildRole("Children",new[]{child}),new HolonChildRole("Zomes",new[]{child})}
            :new[]{new HolonChildRole("Children",new[]{child})});
        create.Should().Throw<ArgumentException>();
    }
}
