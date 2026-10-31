using FluentAssertions;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinSelectionTests
{
    public sealed class Dto
    {
        public static int Constructors, Setters;
        public Dto() { Constructors++; }
        public string Name { get; set; } = "";
        private int? revision;
        public int? Revision { get => revision; set { Setters++; revision = value; } }
        public bool HasRight { get; set; }
    }

    public sealed class Wide
    {
        public int P0 { get; set; }
        public int P1 { get; set; }
        public int P2 { get; set; }
        public int P3 { get; set; }
        public int P4 { get; set; }
        public int P5 { get; set; }
        public int P6 { get; set; }
        public int P7 { get; set; }
        public int P8 { get; set; }
        public int P9 { get; set; }
        public int P10 { get; set; }
        public int P11 { get; set; }
        public int P12 { get; set; }
        public int P13 { get; set; }
        public int P14 { get; set; }
        public int P15 { get; set; }
        public int P16 { get; set; }
        public int P17 { get; set; }
        public int P18 { get; set; }
        public int P19 { get; set; }
        public int P20 { get; set; }
        public int P21 { get; set; }
        public int P22 { get; set; }
        public int P23 { get; set; }
        public int P24 { get; set; }
        public int P25 { get; set; }
        public int P26 { get; set; }
        public int P27 { get; set; }
        public int P28 { get; set; }
        public int P29 { get; set; }
        public int P30 { get; set; }
        public int P31 { get; set; }
        public int P32 { get; set; }
    }

    public sealed class OperatorHolon : Holon
    {
        public static int Calls;
        public static bool operator ==(OperatorHolon? left, OperatorHolon? right) { Calls++; return false; }
        public static bool operator !=(OperatorHolon? left, OperatorHolon? right) { Calls++; return true; }
        public override bool Equals(object? value) => ReferenceEquals(this,value);
        public override int GetHashCode() => base.GetHashCode();
    }

    private static CosmicJoinQuery<Holon,Holon,Guid> Query(bool left = true)
    {
        var first = CosmicQuery<Holon>.Create();
        var second = CosmicQuery<Holon>.Create();
        return (left ? first.LeftJoin(second, row => row.Id, row => row.ParentHolonId)
            : first.Join(second, row => row.Id, row => row.ParentHolonId)).Result;
    }

    [Fact]
    public void FlatDtoCapturePreservesJoinAndBothSidesWithoutConstructingOrAssigning()
    {
        Dto.Constructors = Dto.Setters = 0;
        var join = Query();
        var result = join.Select((left, right) => new Dto {
            Name=left.Name, Revision=(int?)right.Version, HasRight=right!=null });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Join.Should().BeSameAs(join);
        Dto.Constructors.Should().Be(0);
        Dto.Setters.Should().Be(0);
        result.Result.Columns.Select(column => column.Source).Should().Equal(
            CosmicJoinColumnSource.Left,CosmicJoinColumnSource.Right,CosmicJoinColumnSource.RightPresence);
        result.Result.Columns[1].Path.ValueType.Should().Be(typeof(int));
        result.Result.Columns[1].ValueType.Should().Be(typeof(int?));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AnonymousSelectionCapturesTheExplicitRightPresencePolarity(bool missing)
    {
        var result = missing ? Query().Select((left, right) => new { Name=right.Name, Flag=right==null })
            : Query().Select((left, right) => new { Name=right.Name, Flag=null!=right });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Columns[1].Source.Should().Be(CosmicJoinColumnSource.RightPresence);
        result.Result.Columns[1].Path.Should().BeNull();
        result.Result.Columns[1].PresenceWhenMissing.Should().Be(missing);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnmatchedValueTypesMustRepresentAbsence(bool left)
    {
        var result = Query(left).Select((first, second) => second.Version);
        result.IsError.Should().Be(left);
        if (left) result.ErrorCode.Should().Be("CQL_JOIN_SELECTION_UNSUPPORTED");
    }

    [Fact]
    public void NullableMetadataAndReferencePathsRemainDistinctFromPresence()
    {
        var result = Query().Select((left, right) => new {
            Optional=CosmicQueryMetadata.Value<int?>(right,"optional"), Text=right.Name, HasRight=right!=null });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Columns[0].ValueType.Should().Be(typeof(int?));
        result.Result.Columns[0].Path.IsMetadata.Should().BeTrue();
        result.Result.Columns[1].Source.Should().Be(CosmicJoinColumnSource.Right);
        result.Result.Columns[2].Source.Should().Be(CosmicJoinColumnSource.RightPresence);
    }

    [Fact]
    public void LiftCannotHideNumericWidening()
    {
        Query().Select((left, right) => (long?)right.Version).IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ComputedAndEntityValuesAreNotDetachedScalarPaths(bool entity)
    {
        if (entity) Query().Select((left, right) => new { Entity=left }).IsError.Should().BeTrue();
        else Query().Select((left, right) => left.Version+right.Version).IsError.Should().BeTrue();
    }

    [Fact]
    public void ReturningAnEntityAsTheEntireSelectionIsRejected()
    {
        Query().Select((left, right) => left).ErrorCode.Should().Be("CQL_JOIN_SELECTION_UNSUPPORTED");
    }

    [Fact]
    public void SelectedApplicationMethodsCannotRunDuringCapture()
    {
        var calls=0;
        Func<int> app=()=>{calls++;return 1;};
        Query().Select((left,right)=>app()).IsError.Should().BeTrue();
        calls.Should().Be(0);
    }

    [Fact]
    public void NestedAndCollectionOutputShapesAreRejected()
    {
        Query().Select((left,right)=>new { Nested=new { left.Name } }).IsError.Should().BeTrue();
        Query().Select((left,right)=>CosmicQueryMetadata.Value<int[]>(right,"array")).IsError.Should().BeTrue();
    }

    [Fact]
    public void NumericLiftOnLeftPreservesTheRawDomain()
    {
        var result=Query().Select((left,right)=>(int?)left.Version);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Columns.Single().Source.Should().Be(CosmicJoinColumnSource.Left);
        result.Result.Columns.Single().Path.ValueType.Should().Be(typeof(int));
        result.Result.Columns.Single().ValueType.Should().Be(typeof(int?));
    }

    [Fact]
    public void FluentJoinErrorsRemainTheOriginalError()
    {
        var failure=new OASISResult<CosmicJoinQuery<Holon,Holon,Guid>> {
            IsError=true, ErrorCode="CQL_JOIN_ACCESS_MISMATCH", Message="actor mismatch" };
        var result=failure.Select((left,right)=>left.Name);
        result.ErrorCode.Should().Be("CQL_JOIN_ACCESS_MISMATCH");
        result.Message.Should().Be("actor mismatch");
    }

    [Fact]
    public void MissingJoinOrSelectorReturnsAnExplicitError()
    {
        CosmicJoinQuery<Holon,Holon,Guid> missing=null!;
        missing.Select((left,right)=>left.Name).ErrorCode.Should().Be("CQL_JOIN_SELECTION_INVALID");
        Query().Select<Holon,Holon,Guid,string>(null!).ErrorCode.Should().Be("CQL_JOIN_SELECTION_INVALID");
    }
    [Fact]
    public void ThirtyThreeOutputColumnsAreRejectedWithoutConstruction()
    {
        var left=Expression.Parameter(typeof(Holon),"left");
        var right=Expression.Parameter(typeof(Holon),"right");
        var version=Expression.Property(left,nameof(Holon.Version));
        var shape=Expression.MemberInit(Expression.New(typeof(Wide)),typeof(Wide).GetProperties()
            .Select(property=>Expression.Bind(property,version)));
        var expression=Expression.Lambda<Func<Holon,Holon,Wide>>(shape,left,right);
        Query().Select(expression).ErrorCode.Should().Be("CQL_JOIN_SELECTION_UNSUPPORTED");
    }

    [Fact]
    public void DuplicateMemberAliasesCannotOverwriteAColumn()
    {
        var left=Expression.Parameter(typeof(Holon),"left");
        var right=Expression.Parameter(typeof(Holon),"right");
        var name=typeof(Dto).GetProperty(nameof(Dto.Name))!;
        var shape=Expression.MemberInit(Expression.New(typeof(Dto)),
            Expression.Bind(name,Expression.Property(left,nameof(Holon.Name))),
            Expression.Bind(name,Expression.Property(right,nameof(Holon.Name))));
        var expression=Expression.Lambda<Func<Holon,Holon,Dto>>(shape,left,right);
        Query().Select(expression).ErrorCode.Should().Be("CQL_JOIN_SELECTION_UNSUPPORTED");
    }

    [Fact]
    public void OverloadedNullComparisonIsNotAStoragePresenceTest()
    {
        OperatorHolon.Calls=0;
        var join=CosmicQuery<Holon>.Create().LeftJoin(CosmicQuery<OperatorHolon>.Create(),
            left=>left.Id,right=>right.ParentHolonId);
        join.Select((left,right)=>right!=null).IsError.Should().BeTrue();
        OperatorHolon.Calls.Should().Be(0);
    }

}