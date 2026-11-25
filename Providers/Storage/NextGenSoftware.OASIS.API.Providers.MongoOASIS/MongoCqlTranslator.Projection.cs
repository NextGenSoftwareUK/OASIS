using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlProjectionCommand
{
    internal BsonDocument[] Pipeline { get; }
    internal bool RequiresMembership { get; }
    internal bool RequiresSnapshot { get; }
    internal bool HasTraversalEnvelope { get; }
    internal bool IsEmpty { get; }
    internal MongoCqlEntityCompositionCommand EntityComposition { get; }
    internal int EntityOrderingKeyCount { get; }
    internal MongoCqlProjectionCommand(BsonDocument[] pipeline, bool membership, bool snapshot, bool traverses, bool empty,
        MongoCqlEntityCompositionCommand entityComposition=null, int entityOrderingKeyCount=0)
    { Pipeline = pipeline; RequiresMembership = membership; RequiresSnapshot = snapshot; HasTraversalEnvelope = traverses; IsEmpty = empty; EntityComposition=entityComposition; EntityOrderingKeyCount=entityOrderingKeyCount; }
}

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlProjectionCommand CompileEntityProjection<T,TResult>(CosmicEntityProjectionPlan<T,TResult> plan,
        int maximumResults) where T:IHolon
    {
        if(plan==null || maximumResults<0 || maximumResults>CosmicProjectionResults.MaximumResultLimit)
            throw new ArgumentException("An admitted bounded entity projection is required.");
        var entity=CompileEntityComposition(plan.Source,Math.Max(1,maximumResults));
        // The source page and every later entity stage are already captured.
        // Replace only the final publication sentinel; never reapply source paging.
        var pipeline=entity.Pipeline.Take(entity.Pipeline.Length-1).Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        pipeline.Add(new("$limit",maximumResults+1));
        var cells=new BsonArray();
        for(var index=0;index<plan.Selection.Query.Columns.Count;index++)
            cells.Add(ProjectionCell(plan.Selection.Query.Columns[index],"cqlep"+index));
        pipeline.Add(new("$project",new BsonDocument { {"_id",0},{"cells",cells} }));
        ValidatePipeline(pipeline);
        return new MongoCqlProjectionCommand(pipeline.ToArray(),entity.RequiresMembership,true,false,false,entity.EntityComposition);
    }

    internal MongoCqlProjectionCommand CompileProjection<T, TResult>(CosmicProjectionQuery<T, TResult> query, int maximumResults) where T : IHolon
    {
        var source = CompileSource(query.Source, out var traverses);
        var fetch = query.Source.Limit.HasValue ? Math.Min(query.Source.Limit.Value, maximumResults + 1) : maximumResults + 1;
        var page = SelectedWindow(query.Source, fetch);
        var cells = new BsonArray();
        for (var index = 0; index < query.Columns.Count; index++)
        {
            cells.Add(ProjectionCell(query.Columns[index], "cqlp" + index));
        }
        page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "cells", cells } }));
        // Reuse the source-owned invalid branch, outside ordering/paging/columns.
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(HasRelationships);
        return new MongoCqlProjectionCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null, traverses, fetch == 0);
    }

    private static List<BsonDocument> SelectedWindow<T>(CosmicQuery<T> query, int? limit) where T : IHolon
    {
        var page = new List<BsonDocument> { new("$project", new BsonDocument("Children", 0)) };
        // Empty source windows still permit protected traversal/membership
        // validation, but no selected value may enter a reduction or decoder.
        if (query.Limit == 0) page.Add(new("$match", new BsonDocument("$expr", Literal(false))));
        if (query.Orderings.Count != 0)
        {
            var wrapped = new BsonDocument { { "_id", 0 }, { "row", "$$ROOT" } };
            var sort = new BsonDocument();
            for (var index = 0; index < query.Orderings.Count; index++)
            {
                var ordering = query.Orderings[index]; var key = "s" + index; var direction = ordering.Descending ? -1 : 1;
                wrapped.Add(key, OrderingKey(ordering.Path, "cqlpsort" + index));
                sort.Add(key + ".rank", direction); sort.Add(key + ".value", direction);
            }
            sort.Add("row.HolonId", 1);
            page.Add(new("$project", wrapped)); page.Add(new("$sort", sort));
        }
        else page.Add(new("$sort", new BsonDocument("HolonId", 1)));
        if (query.Offset != 0) page.Add(new("$skip", query.Offset));
        if (limit.HasValue) page.Add(new("$limit", Math.Max(1, limit.Value)));
        if (query.Orderings.Count != 0) page.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
        return page;
    }

    internal static BsonValue UtcDatePartFromTicks(CosmicProjectionFunction function,BsonValue ticks)
    {
                var unit=function switch {
                    CosmicProjectionFunction.UtcHour=>TimeSpan.TicksPerHour,
                    CosmicProjectionFunction.UtcMinute=>TimeSpan.TicksPerMinute,
                    CosmicProjectionFunction.UtcSecond=>TimeSpan.TicksPerSecond,
                    _=>TimeSpan.TicksPerDay };
                if(unit!=TimeSpan.TicksPerDay)
                {
                    var span=function==CosmicProjectionFunction.UtcHour ? TimeSpan.TicksPerDay
                        : function==CosmicProjectionFunction.UtcMinute ? TimeSpan.TicksPerHour : TimeSpan.TicksPerMinute;
                    var remainder=Op("$mod",ticks,Literal(span));
                    // Divide an exactly divisible bounded integer, never floating epoch ticks.
                    return new BsonDocument("$toInt",Op("$divide",
                        Op("$subtract",remainder,Op("$mod",remainder,Literal(unit))),Literal(unit)));
                }
                var days=new BsonDocument("$toLong",Op("$divide",
                    Op("$subtract",ticks,Op("$mod",ticks,Literal(TimeSpan.TicksPerDay))),Literal(TimeSpan.TicksPerDay)));
                var date=new BsonDocument("$toDate",Op("$multiply",
                    Op("$subtract",days,Literal(DateTime.UnixEpoch.Ticks/TimeSpan.TicksPerDay)),
                    Literal((long)86400000)));
                var operation=function switch {
                    CosmicProjectionFunction.UtcYear=>"$year",CosmicProjectionFunction.UtcMonth=>"$month",
                    CosmicProjectionFunction.UtcDay=>"$dayOfMonth",CosmicProjectionFunction.UtcDayOfYear=>"$dayOfYear",
                    _=>throw new NotSupportedException("UTC date part is not registered.") };
                return new BsonDocument(operation,new BsonDocument{{"date",date},{"timezone","UTC"}});
                }

    private static BsonValue ProjectionCell(CosmicProjectionColumn column, string prefix)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (column.Function == CosmicProjectionFunction.None) return ProjectionCell(column.Path, prefix);
        if (column.IsUtcDatePart)
        {
            var output=Nullable.GetUnderlyingType(column.ValueType)??column.ValueType;
            if(column.Path.ValueType!=typeof(DateTime) || output!=typeof(int))
                throw new NotSupportedException("UTC date parts require DateTime input and Int32 output.");
            var utcName=prefix+"utcPart";
            var cell="$$"+utcName;
            var value=(BsonValue)(cell+".v");
            var ticks=column.Path.IsMetadata ? value
                : Op("$add",Op("$multiply",new BsonDocument("$toLong",value),Literal(TimeSpan.TicksPerMillisecond)),
                    Literal(DateTime.UnixEpoch.Ticks));

            return SingleLet(utcName,ProjectionCell(column.Path,prefix+"utcSource"),new BsonDocument{
                {"s",cell+".s"},{"v",Choose(Op("$eq",cell+".s",Literal(2)),UtcDatePartFromTicks(column.Function,ticks),Literal(BsonNull.Value))}});
        }
        if (column.Function == CosmicProjectionFunction.Int32Affine)
        {
            if(column.Path.ValueType != typeof(int) || column.ValueType != typeof(int) || !column.Int32Multiplier.HasValue || !column.Int32Offset.HasValue)
                throw new NotSupportedException("Native Int32 arithmetic requires an exact Int32 path and captured coefficients.");
            var arithmeticName = prefix + "affine";
            var cell = "$$" + arithmeticName;
            // 64-bit arithmetic, then an explicit Int32 range check: overflow is an invalid cell.
            BsonValue computed = Op("$add", Op("$multiply", new BsonDocument("$toLong", cell + ".v"), Literal((long)column.Int32Multiplier.Value)), Literal((long)column.Int32Offset.Value));
            // $mod keeps the dividend's sign and $trunc($divide) truncates toward zero, matching C#.
            if (column.Int32Divisor.HasValue)
                computed = column.Int32IsModulo ? Op("$mod", computed, Literal((long)column.Int32Divisor.Value))
                    : new BsonDocument("$toLong", new BsonDocument("$trunc", Op("$divide", computed, Literal((long)column.Int32Divisor.Value))));
            var present = Op("$eq", cell + ".s", Literal(2));
            var inRange = Op("$and", Op("$gte", computed, Literal((long)int.MinValue)), Op("$lte", computed, Literal((long)int.MaxValue)));
            return SingleLet(arithmeticName, ProjectionCell(column.Path, prefix + "source"), new BsonDocument {
                {"s", Choose(present, Choose(inRange, Literal(2), Literal(3)), cell + ".s")},
                {"v", Choose(Op("$and", present, inRange), new BsonDocument("$toInt", computed), BsonNull.Value)}
            });
        }
        if (column.Function == CosmicProjectionFunction.TextToInt32Case)
        {
            if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(int)
                || column.Int32Cases == null || column.Int32Cases.Count == 0 || !column.Int32Replacement.HasValue)
                throw new NotSupportedException("Native text-to-Int32 CASE requires an exact String path and captured literal branches.");
            var numericName = prefix + "numericCase";
            var numericPresent = Op("$eq", "$$"+numericName+".s", Literal(2));
            var numericBranches = new BsonArray();
            foreach (var entry in column.Int32Cases)
                numericBranches.Add(new BsonDocument{{"case",Op("$eq","$$"+numericName+".v",Literal(entry.Key))},{"then",Literal(entry.Value)}});
            var numericSelected = new BsonDocument("$switch",new BsonDocument{{"branches",numericBranches},{"default",Literal(column.Int32Replacement.Value)}});
            return SingleLet(numericName,ProjectionCell(column.Path,prefix+"source"),new BsonDocument {
                {"s",Choose(numericPresent,Literal(2),Literal(3))},
                {"v",Choose(numericPresent,numericSelected,BsonNull.Value)}
            });
        }
        if (column.Function == CosmicProjectionFunction.TextValueCase)
        {
            if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(string)
                || column.TextCases == null || column.TextCases.Count == 0 || column.WhenFalse == null)
                throw new NotSupportedException("Native multi-branch text CASE requires an exact String path and captured literal branches.");
            var caseName = prefix + "textCase";
            var present = Op("$eq", "$$"+caseName+".s", Literal(2));
            var branches = new BsonArray();
            foreach (var entry in column.TextCases)
                branches.Add(new BsonDocument{{"case",Op("$eq","$$"+caseName+".v",Literal(entry.Key))},{"then",Literal(entry.Value)}});
            var selected = new BsonDocument("$switch",new BsonDocument{{"branches",branches},{"default",Literal(column.WhenFalse)}});
            return SingleLet(caseName,ProjectionCell(column.Path,prefix+"source"),new BsonDocument {
                {"s",Choose(present,Literal(2),Literal(3))},
                {"v",Choose(present,selected,BsonNull.Value)}
            });
        }
        if (column.Function == CosmicProjectionFunction.BooleanTextCase)
        {
            if(column.Path.ValueType != typeof(bool) || column.ValueType != typeof(string)
                || column.WhenTrue == null || column.WhenFalse == null)
                throw new NotSupportedException("Native conditional text requires an exact Boolean path and captured literal branches.");
            var conditionalName = prefix + "conditionalBoolean";
            var valid = Op("$eq", "$$"+conditionalName+".s", Literal(2));
            return SingleLet(conditionalName,ProjectionCell(column.Path,prefix+"source"),new BsonDocument {
                {"s",Choose(valid,Literal(2),Literal(3))},
                {"v",Choose(valid,Choose(Op("$eq","$$"+conditionalName+".v",Literal(true)),Literal(column.WhenTrue),Literal(column.WhenFalse)),BsonNull.Value)}
            });
        }
        if ((column.Function == CosmicProjectionFunction.TextCoalesce || column.Function == CosmicProjectionFunction.Int32Coalesce))
        {
            var coalesceIsText=column.Function==CosmicProjectionFunction.TextCoalesce;
              if(coalesceIsText
                  ? column.Path.ValueType!=typeof(string) || column.ValueType!=typeof(string) || column.NullReplacement==null
                  : column.Path.ValueType!=typeof(int?) || column.ValueType!=typeof(int) || !column.Int32Replacement.HasValue)
                throw new NotSupportedException("Native coalescing requires exact source and captured replacement domains.");
            var coalesced = prefix + "coalescedText";
            var absent = Op("$lte", "$$" + coalesced + ".s", Literal(1));
            return SingleLet(coalesced,ProjectionCell(column.Path,prefix+"source"),new BsonDocument {
                {"s",Choose(absent,Literal(2),"$$"+coalesced+".s")},
                {"v",Choose(absent,(coalesceIsText ? Literal(column.NullReplacement) : Literal(column.Int32Replacement.Value)),"$$"+coalesced+".v")}
            });
        }
        if (column.Function != CosmicProjectionFunction.Utf16Length ||
            column.Path.ValueType != typeof(string) ||
            (Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType) != typeof(int))
            throw new NotSupportedException("The computed projection has no admitted native implementation.");
        var name = prefix + "inputCell";
        return SingleLet(name, ProjectionCell(column.Path, prefix + "source"), new BsonDocument
        {
            { "s", "$$" + name + ".s" },
            { "v", Choose(Op("$eq", "$$" + name + ".s", Literal(2)),
                Utf16LengthUnits("$$" + name + ".v"), Literal(BsonNull.Value)) }
        });
    }

    private static BsonValue ProjectionCell(CosmicQueryPath path, string prefix)
    {
        BsonValue Cell(BsonValue raw) => WithMetadataOperand(raw, path, value =>
        {
            var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
            var valid = type == typeof(Guid) ? GuidDomain(value) : Domain(value, path.ValueType, path.IsMetadata);
            if (path.IsMetadata && type == typeof(DateTime))
            {
                var pointer = "/" + string.Join("/", path.Segments.Select(segment => segment.Replace("~", "~0").Replace("/", "~1")));
                var matches = new BsonDocument("$filter", new BsonDocument
                {
                    { "input", Op("$ifNull", "$MetaDataExactUtcTicks", Literal(new BsonArray())) },
                    { "as", "cqlprojectiondates" },
                    { "cond", Op("$eq", "$$cqlprojectiondates.Path", Literal(pointer)) }
                });
                // Precision ownership is singular. Do not choose the first
                // conflicting companion when projecting an exact timestamp.
                valid = Op("$and", valid, Op("$lte", new BsonDocument("$size", matches), Literal(1)));
            }
            var state = Choose(Op("$eq", Type(raw), Literal("missing")), Literal(0),
                Choose(Op("$eq", Type(raw), Literal("null")), Literal(1), Choose(valid, Literal(2), Literal(3))));
            return new BsonDocument { { "s", state }, { "v", Choose(valid, value, Literal(BsonNull.Value)) } };
        }, prefix);
        return path.IsMetadata ? ProjectionMetadata(path.Segments, Cell, prefix) : Cell(Canonical(path));
    }

    private static BsonValue ProjectionMetadata(IReadOnlyList<string> segments, Func<BsonValue, BsonValue> cell, string prefix)
    {
        if (segments.Any(segment => segment.IndexOf('\0') >= 0)) throw new NotSupportedException("MongoDB metadata paths cannot contain NUL.");
        var missing = new BsonDocument { { "s", Literal(0) }, { "v", Literal(BsonNull.Value) } };
        var invalid = new BsonDocument { { "s", Literal(3) }, { "v", Literal(BsonNull.Value) } };
        BsonValue Walk(BsonValue input, int index)
        {
            if (index == segments.Count)
                return new BsonDocument { { "s", Literal(2) }, { "v", input } };
            var current = prefix + "n" + index;
            var matches = prefix + "m" + index;
            var entry = prefix + "e" + index;
            var first = prefix + "f" + index;
            BsonValue value = "$$" + current;
            var objectEntries = new BsonDocument("$objectToArray", value);
            var entries = Choose(Op("$eq", Type(value), Literal("object")), objectEntries,
                index == 0 ? Choose(Op("$eq", Type(value), Literal("array")), value, Literal(new BsonArray())) : Literal(new BsonArray()));
            var key = Choose(Op("$eq", Type("$$" + entry), Literal("array")),
                Op("$arrayElemAt", "$$" + entry, Literal(0)), "$$" + entry + ".k");
            var filtered = new BsonDocument("$filter", new BsonDocument
            { { "input", entries }, { "as", entry }, { "cond", Op("$eq", key, Literal(segments[index])) } });
            var candidate = "$$" + first;
            var isArray = Op("$eq", Type(candidate), Literal("array"));
            var wellFormed = Choose(isArray, Op("$eq", new BsonDocument("$size", candidate), Literal(2)),
                Op("$ne", Type(candidate + ".v"), Literal("missing")));
            var child = Choose(isArray, Op("$arrayElemAt", candidate, Literal(1)), candidate + ".v");
            var selected = new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument(first, Op("$arrayElemAt", "$$" + matches, Literal(0))) },
                { "in", Choose(wellFormed, Walk(child, index + 1), invalid) }
            });
            var body = new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument(matches, filtered) },
                { "in", Choose(Op("$eq", new BsonDocument("$size", "$$" + matches), Literal(0)), missing,
                    Choose(Op("$eq", new BsonDocument("$size", "$$" + matches), Literal(1)), selected, invalid)) }
            });
            return new BsonDocument("$let", new BsonDocument { { "vars", new BsonDocument(current, input) }, { "in", body } });
        }
        // Resolve storage shape first. The typed conversion must not inherit the nesting of the metadata walk.
        var resolved = prefix + "resolved";
        return SingleLet(resolved, Walk("$MetaData", 0), Choose(
            Op("$eq", "$$" + resolved + ".s", Literal(2)), cell("$$" + resolved + ".v"), "$$" + resolved));
    }
}
