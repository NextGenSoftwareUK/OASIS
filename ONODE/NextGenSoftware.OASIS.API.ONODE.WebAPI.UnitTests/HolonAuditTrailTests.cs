using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

/// <summary>The generic holon audit trail: created/updated/deleted/restored, JSON list diff, skip keys, labels, clipping, unknown actor.</summary>
public sealed class HolonAuditTrailTests
{
    private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Bob = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime T0 = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    private static Holon V(int version, Guid by, string name = "Student", Dictionary<string, object> meta = null, DateTime? deleted = null, Guid? deletedBy = null)
    {
        var h = new Holon
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = name,
            Version = version,
            CreatedByAvatarId = Alice,
            CreatedDate = T0,
            ModifiedByAvatarId = by,
            ModifiedDate = T0.AddHours(version),
            MetaData = meta ?? new Dictionary<string, object>()
        };
        if (deleted.HasValue) { h.DeletedDate = deleted.Value; h.DeletedByAvatarId = deletedBy ?? by; }
        return h;
    }

    private static HolonAuditTrailOptions Opts(Func<Guid, string> names = null) =>
        new HolonAuditTrailOptions { AvatarNameResolver = names ?? (id => id == Alice ? "Alice" : id == Bob ? "Bob" : null) };

    [Fact]
    public void FirstVersionIsCreatedAndNewestComesFirst()
    {
        var trail = HolonAuditTrail.Build(new[] { V(2, Bob, "Student B"), V(1, Alice) }, Opts());
        Assert.Equal(new[] { 2, 1 }, trail.Select(e => e.Version).ToArray());
        Assert.Equal("created", trail[1].Action);
        Assert.Equal("updated", trail[0].Action);
        Assert.Equal("Alice", trail[1].AvatarName);
        Assert.Equal("Bob", trail[0].AvatarName);
        var change = Assert.Single(trail[0].Changes);
        Assert.Equal("name", change.Field);
        Assert.Equal("Student", change.OldValue);
        Assert.Equal("Student B", change.NewValue);
    }

    [Fact]
    public void DeleteThenRestoreAreReported()
    {
        var trail = HolonAuditTrail.Build(new[]
        {
            V(1, Alice), V(2, Bob, deleted: T0.AddHours(2), deletedBy: Bob), V(3, Alice)
        }, Opts());
        Assert.Equal(new[] { "restored", "deleted", "created" }, trail.Select(e => e.Action).ToArray());
        Assert.Equal(Bob, trail[1].AvatarId);
    }

    [Fact]
    public void MetadataChangesAreFieldLevelAndLabelled()
    {
        var a = V(1, Alice, meta: new Dictionary<string, object> { ["grade"] = "A" });
        var b = V(2, Bob, meta: new Dictionary<string, object> { ["grade"] = "B", ["note"] = "new" });
        var options = Opts();
        options.FieldLabels = new Dictionary<string, string> { ["grade"] = "Final grade" };
        var trail = HolonAuditTrail.Build(new[] { a, b }, options);
        var changes = trail[0].Changes.ToDictionary(c => c.Field);
        Assert.Equal("A", changes["Final grade"].OldValue);
        Assert.Equal("B", changes["Final grade"].NewValue);
        Assert.Null(changes["note"].OldValue);
        Assert.Equal("new", changes["note"].NewValue);
    }

    [Fact]
    public void SkipKeysAreIgnoredCaseInsensitively()
    {
        var a = V(1, Alice, meta: new Dictionary<string, object> { ["lastSeen"] = "1" });
        var b = V(2, Bob, meta: new Dictionary<string, object> { ["lastSeen"] = "2" });
        var options = Opts();
        options.SkipKeys = new[] { "LASTSEEN" };
        Assert.Empty(HolonAuditTrail.Build(new[] { a, b }, options)[0].Changes);
    }

    [Fact]
    public void JsonListsWithIdsAreSummarisedNotDumped()
    {
        var a = V(1, Alice, meta: new Dictionary<string, object> { ["goals"] = "[{\"id\":\"g1\",\"name\":\"Wh blends\"},{\"id\":\"g2\",\"name\":\"R words\"}]" });
        var b = V(2, Bob, meta: new Dictionary<string, object> { ["goals"] = "[{\"id\":\"g1\",\"name\":\"Wh blends 2\"},{\"id\":\"g3\",\"name\":\"L words\"}]" });
        var change = Assert.Single(HolonAuditTrail.Build(new[] { a, b }, Opts())[0].Changes);
        Assert.Equal("goals", change.Field);
        Assert.Contains("added 1", change.NewValue);
        Assert.Contains("L words [g3]", change.NewValue);
        Assert.Contains("removed 1", change.NewValue);
        Assert.Contains("R words [g2]", change.NewValue);
        Assert.Contains("changed 1", change.NewValue);
        Assert.DoesNotContain("{\"id\"", change.NewValue);
    }

    [Fact]
    public void LongValuesAreClipped()
    {
        var a = V(1, Alice, meta: new Dictionary<string, object> { ["text"] = "x" });
        var b = V(2, Bob, meta: new Dictionary<string, object> { ["text"] = new string('y', 500) });
        var options = Opts();
        options.MaxValueLength = 50;
        var change = Assert.Single(HolonAuditTrail.Build(new[] { a, b }, options)[0].Changes);
        Assert.Equal(50, change.NewValue.Length);
        Assert.EndsWith("…", change.NewValue);
    }

    [Fact]
    public void UnknownOrThrowingActorGivesBlankName()
    {
        var trail = HolonAuditTrail.Build(new[] { V(1, Guid.NewGuid()) }, Opts(_ => throw new InvalidOperationException("no read")));
        Assert.Equal("", trail[0].AvatarName);
        var none = HolonAuditTrail.Build(new[] { V(1, Guid.NewGuid()) }, new HolonAuditTrailOptions());
        Assert.Equal("", none[0].AvatarName);
    }

    [Fact]
    public void IdenticalNonJsonValuesProduceNoChange()
    {
        var a = V(1, Alice, meta: new Dictionary<string, object> { ["k"] = "same" });
        var b = V(2, Bob, meta: new Dictionary<string, object> { ["k"] = "same" });
        Assert.Empty(HolonAuditTrail.Build(new[] { a, b }, Opts())[0].Changes);
    }
}
