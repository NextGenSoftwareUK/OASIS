using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    internal IReadOnlyList<(string State, string Value)> TranslateProjection(IReadOnlyList<CosmicProjectionColumn> columns)
    {
        var projected = new List<(string, string)>();
        foreach (var column in columns)
        {
            projected.Add(TranslateProjectionColumn(column));
        }
        return projected.AsReadOnly();
    }

    internal (string State, string Value) TranslateProjectionColumn(CosmicProjectionColumn column)
    {
        if (column == null) throw new System.ArgumentNullException(nameof(column));
        if (column.Function == CosmicProjectionFunction.None) return TranslateProjectionPath(column.Path);
        if(column.IsUtcDatePart)
        {
            if(column.Path.ValueType!=typeof(System.DateTime) || (System.Nullable.GetUnderlyingType(column.ValueType)??column.ValueType)!=typeof(int))
                throw new System.NotSupportedException("UTC date parts require DateTime input and Int32 output.");
            var utcSource=TranslateProjectionPath(column.Path);
            return (utcSource.State,"CASE WHEN ("+utcSource.State+")=2 THEN "+
                NeoCqlUtcDateParts.Value(column.Function,"datetime("+utcSource.Value+")")+" ELSE null END");
        }
        if (column.Function == CosmicProjectionFunction.Int64Affine)
        {
            if(column.Path.ValueType != typeof(long) || column.ValueType != typeof(long) || !column.Int64Multiplier.HasValue || !column.Int64Offset.HasValue)
                throw new System.NotSupportedException("Native Int64 arithmetic requires an exact Int64 path and captured coefficients.");
            var longSource=TranslateProjectionPath(column.Path);
            // Cypher integers are 64-bit and overflow raises a native error, so a result is never wrapped.
            return (longSource.State,"CASE WHEN ("+longSource.State+")=2 THEN (("+longSource.Value+")*"+Bind(column.Int64Multiplier.Value)+"+"+Bind(column.Int64Offset.Value)+") ELSE null END");
        }
        if (column.Function == CosmicProjectionFunction.DoubleAffine)
        {
            if(column.Path.ValueType != typeof(double) || column.ValueType != typeof(double) || !column.DoubleMultiplier.HasValue || !column.DoubleOffset.HasValue)
                throw new System.NotSupportedException("Native Double arithmetic requires an exact Double path and captured coefficients.");
            var doubleSource=TranslateProjectionPath(column.Path);
            var doubleComputed="((toFloat("+doubleSource.Value+"))*"+Bind(column.DoubleMultiplier.Value)+"+"+Bind(column.DoubleOffset.Value)+")";
            var finite="coalesce(abs("+doubleComputed+") <= 1.7976931348623157e308,false)";
            return ("CASE WHEN ("+doubleSource.State+")=2 AND NOT "+finite+" THEN 3 ELSE ("+doubleSource.State+") END",
                "CASE WHEN ("+doubleSource.State+")=2 AND "+finite+" THEN "+doubleComputed+" ELSE null END");
        }
        if (column.Function == CosmicProjectionFunction.Int32Affine)
        {
            if(column.Path.ValueType != typeof(int) || column.ValueType != typeof(int) || !column.Int32Multiplier.HasValue || !column.Int32Offset.HasValue)
                throw new System.NotSupportedException("Native Int32 arithmetic requires an exact Int32 path and captured coefficients.");
            var arithmeticSource=TranslateProjectionPath(column.Path);
            var computed="(("+arithmeticSource.Value+")*"+Bind((long)column.Int32Multiplier.Value)+"+"+Bind((long)column.Int32Offset.Value)+")";
            // Cypher integer division and remainder truncate toward zero, matching C#.
            if(column.Int32Divisor.HasValue)computed="("+computed+(column.Int32IsModulo?" % ":" / ")+Bind((long)column.Int32Divisor.Value)+")";
            return ("CASE WHEN ("+arithmeticSource.State+")=2 AND ("+computed+"<-2147483648 OR "+computed+">2147483647) THEN 3 ELSE ("+arithmeticSource.State+") END",
                "CASE WHEN ("+arithmeticSource.State+")=2 AND "+computed+">=-2147483648 AND "+computed+"<=2147483647 THEN "+computed+" ELSE null END");
        }
        if (column.Function == CosmicProjectionFunction.TextToInt32Case)
        {
            if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(int)
                || column.Int32Cases == null || column.Int32Cases.Count == 0 || !column.Int32Replacement.HasValue)
                throw new System.NotSupportedException("Native text-to-Int32 CASE requires an exact String path and captured literal branches.");
            var numericSource=TranslateProjectionPath(column.Path);
            var numericBranches=string.Concat(System.Linq.Enumerable.Select(column.Int32Cases,entry=>" WHEN "+Bind(entry.Key)+" THEN "+Bind((long)entry.Value)));
            return ("CASE WHEN ("+numericSource.State+")=2 THEN 2 ELSE 3 END",
                "CASE WHEN ("+numericSource.State+")=2 THEN CASE ("+numericSource.Value+")"+numericBranches+" ELSE "+Bind((long)column.Int32Replacement.Value)+" END ELSE NULL END");
        }
        if (column.Function == CosmicProjectionFunction.TextValueCase)
        {
            if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(string)
                || column.TextCases == null || column.TextCases.Count == 0 || column.WhenFalse == null)
                throw new System.NotSupportedException("Native multi-branch text CASE requires an exact String path and captured literal branches.");
            var caseSource=TranslateProjectionPath(column.Path);
            var branches=string.Concat(System.Linq.Enumerable.Select(column.TextCases,entry=>" WHEN "+Bind(entry.Key)+" THEN "+Bind(entry.Value)));
            return ("CASE WHEN ("+caseSource.State+")=2 THEN 2 ELSE 3 END",
                "CASE WHEN ("+caseSource.State+")=2 THEN CASE ("+caseSource.Value+")"+branches+" ELSE "+Bind(column.WhenFalse)+" END ELSE NULL END");
        }
        if (column.Function == CosmicProjectionFunction.BooleanTextCase)
        {
            if(column.Path.ValueType != typeof(bool) || column.ValueType != typeof(string)
                || column.WhenTrue == null || column.WhenFalse == null)
                throw new System.NotSupportedException("Native conditional text requires an exact Boolean path and captured literal branches.");
            var conditional=TranslateProjectionPath(column.Path);
            var whenTrue=Bind(column.WhenTrue);var whenFalse=Bind(column.WhenFalse);
            return ("CASE WHEN ("+conditional.State+")=2 THEN 2 ELSE 3 END",
                "CASE WHEN ("+conditional.State+")=2 THEN CASE WHEN ("+conditional.Value+")=true THEN "+whenTrue+" ELSE "+whenFalse+" END ELSE NULL END");
        }
        if ((column.Function == CosmicProjectionFunction.TextCoalesce || column.Function == CosmicProjectionFunction.Int32Coalesce))
        {
            var coalesceIsText=column.Function==CosmicProjectionFunction.TextCoalesce;
              if(coalesceIsText
                  ? column.Path.ValueType!=typeof(string) || column.ValueType!=typeof(string) || column.NullReplacement==null
                  : column.Path.ValueType!=typeof(int?) || column.ValueType!=typeof(int) || !column.Int32Replacement.HasValue)
                throw new System.NotSupportedException("Native coalescing requires exact source and captured replacement domains.");
            var input=TranslateProjectionPath(column.Path);var replacement=(coalesceIsText ? Bind(column.NullReplacement) : Bind(column.Int32Replacement.Value));
            return ("CASE WHEN ("+input.State+") IN [0,1] THEN 2 ELSE ("+input.State+") END",
                "CASE WHEN ("+input.State+") IN [0,1] THEN "+replacement+" ELSE ("+input.Value+") END");
        }
        if (column.Function != CosmicProjectionFunction.Utf16Length ||
            column.Path.ValueType != typeof(string) ||
            (System.Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType) != typeof(int))
            throw new System.NotSupportedException("The computed projection has no admitted native implementation.");
        var source = TranslateProjectionPath(column.Path);
        var supplementary = Bind(@"[\x{10000}-\x{10FFFF}]");
        var units = Utf16LengthUnits(source.Value, supplementary);
        return (source.State, "CASE WHEN (" + source.State + ")=2 THEN " + units + " ELSE null END");
    }

    internal (string State, string Value) TranslateProjectionPath(CosmicQueryPath path)
    {
        var operand = Operand(path);
        var missing = path.IsMetadata ? _projection + "[" + Bind(NeoCqlProjection.PathKey(path.Segments) + "_kind") + "] IS NULL" : "false";
        var state = "CASE WHEN " + missing + " THEN 0 WHEN " + operand.Null + " THEN 1 WHEN "
            + Total(operand.Guard) + " THEN 2 ELSE 3 END";
        var value = "CASE WHEN " + Total(operand.Guard) + " THEN " + operand.Value + " ELSE null END";
        return (state, value);
    }
}
