using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static string FloatingAggregateValue(string parts, string count, bool average, bool single, Func<object, string> bind)
        => new FloatingAggregateCompiler(single, bind).Compile(parts, count, average);

    // Every intermediate remains Int64. In particular, divide the prefix words
    // before recombining the final coefficient: the unshifted prefix can have
    // 82 significant bits even though the final Double coefficient needs53.
    private sealed class FloatingAggregateCompiler
    {
        private readonly int width;
        private readonly int precision;
        private readonly int maximumShift;
        private readonly string smallPowers;
        private readonly string outputPowers;
        private int sequence;

        internal FloatingAggregateCompiler(bool single, Func<object, string> bind)
        {
            width = single ? CosmicFloatingAggregateResults.SingleLimbCount : CosmicFloatingAggregateResults.DoubleLimbCount;
            precision = single ? 24 : 53;
            maximumShift = single ? 253 : 2045;
            var minimumExponent = single ? -149 : -1074;
            smallPowers = bind(Enumerable.Range(0, 30).Select(bit => 1L << bit).ToArray());
            outputPowers = bind(Enumerable.Range(0, maximumShift + 1).Select(shift => Math.Pow(2, minimumExponent + shift)).ToArray());
        }

        private string Name() => "cqlfp" + sequence++;
        private string Let(string expression, Func<string, string> body)
        {
            var name = Name();
            return "head([" + name + " IN [" + expression + "] | " + body(name) + "])";
        }
        private string Reduce(string input, string initial, Func<string, string, string> body)
        {
            var state = Name(); var item = Name();
            return "reduce(" + state + "=" + initial + "," + item + " IN " + input + " | " + body(state, item) + ")";
        }
        private static string Choose(string condition, string yes, string no)
            => "CASE WHEN " + condition + " THEN " + yes + " ELSE " + no + " END";

        private string Normalize(string parts)
            => Reduce("reverse(" + parts + ")", "{carry:0,digits:[]}", (state, word) =>
                Let(word + "+" + state + ".carry", value => Let(value + "%1073741824", raw =>
                Let(Choose(raw + "<0", raw + "+1073741824", raw), digit =>
                    "{carry:(" + value + "-" + digit + ")/1073741824,digits:[" + digit + "]+" + state + ".digits}"))));

        private string Divide(string words, string denominator)
            => Reduce(words, "{q:[],r:0}", (state, word) =>
                Let(state + ".r*1073741824+" + word, value =>
                    "{q:" + state + ".q+[" + value + "/(" + denominator + ")],r:" + value + "% (" + denominator + ")}"));

        private string Shift(string words)
            => Let("head([cfIndex IN range(0," + width + ") WHERE " + words + "[cfIndex]<>0])", first =>
                Choose(first + " IS NULL", "0", Let(words + "[" + first + "]", leading =>
                Let(Reduce("range(0,29)", "0", (bits, exponent) =>
                    Choose(leading + ">=" + smallPowers + "[" + exponent + "]", exponent + "+1", bits)), bits =>
                Let("30*(" + width + "-" + first + ")+" + bits + "-" + precision, excess =>
                    Choose(excess + ">0", excess, "0"))))));

        private string Increment(string words, string shift, string coefficient, string remainder, string denominator)
        {
            var odd = coefficient + "%2=1";
            return Choose(shift + "=0", "(" + remainder + "*2>" + denominator + " OR ("
                + remainder + "*2=" + denominator + " AND " + odd + "))",
                Let(shift + "-1", guardPosition => Let(guardPosition + "/30", guardWords =>
                Let(width + "-" + guardWords, guardIndex =>
                Let(smallPowers + "[" + guardPosition + "%30]", guardPower =>
                Let(words + "[" + guardIndex + "]", guardWord =>
                    "((" + guardWord + "/" + guardPower + ")%2=1 AND (" + odd + " OR " + remainder + "<>0 OR "
                    + guardWord + "%" + guardPower + "<>0 OR any(cfSticky IN " + words + "[(" + guardIndex + "+1)..] WHERE cfSticky<>0)))"))))));
        }

        private string Round(string words, string remainder, string denominator, string negative)
            => Let(Shift(words), shift => Choose(shift + ">" + maximumShift, "null",
                Let(shift + "/30", removedWords => Let(shift + "%30", removedBits =>
                Let(words + "[0..(" + (width + 1) + "-" + removedWords + ")]", prefix =>
                Let(Divide(prefix, smallPowers + "[" + removedBits + "]"), shifted =>
                Let(Reduce(shifted + ".q", "0", (coefficient, word) => coefficient + "*1073741824+" + word), coefficient =>
                Let(coefficient + "+" + Choose(Increment(words, shift, coefficient, remainder, denominator), "1", "0"), rounded =>
                    Choose(shift + "=" + maximumShift + " AND " + rounded + ">" + ((1L << precision) - 1), "null",
                        Choose(rounded + "=0", "0.0", "(" + Choose(negative, "-1.0", "1.0")
                            + ")*toFloat(" + rounded + ")*" + outputPowers + "[" + shift + "]"))))))))));

        internal string Compile(string parts, string count, bool average)
            => Let(parts, input => Let(count, participation =>
                Choose(participation + (average ? ">0" : ">=0") + " AND " + participation + "<=2147483647",
                    Let(Normalize(input), signed => Let(signed + ".carry<0", negative =>
                    Let(Choose(negative, Normalize("[cfWord IN " + input + " | -cfWord]"), signed), magnitude =>
                    Let("[" + magnitude + ".carry]+" + magnitude + ".digits", words =>
                    Let(average ? participation : "1", denominator =>
                    Let(Divide(words, denominator), divided =>
                        Round(divided + ".q", divided + ".r", denominator, negative))))))), "null")));
    }
}
