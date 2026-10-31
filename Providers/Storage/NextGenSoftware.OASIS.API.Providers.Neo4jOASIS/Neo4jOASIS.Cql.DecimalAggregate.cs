using System;
using System.Linq;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static string DecimalAggregateKey(string parts, string count, bool average)
        => new DecimalAggregateCompiler().Compile(parts, count, average);

    // Decimal source keys contain sign plus 57 scaled28 digits. Decode seven
    // signed base1e9 words without converting the source to Neo4j Float.
    internal static string DecimalAggregateParts(string key)
    {
        var words = string.Join(",", Enumerable.Range(0, 7).Select(index =>
            "toInteger(substring(cdMagnitude," + index * 9 + ",9))"));
        var magnitude = "CASE WHEN left(cdKey,1)='1' THEN substring(cdKey,1) ELSE "
            + "reduce(cdText='',cdIndex IN range(1,57) | cdText+substring('9876543210',"
            + "toInteger(substring(cdKey,cdIndex,1)),1)) END";
        return "head([cdKey IN [" + key + "] | head([cdMagnitude IN ['000000'+(" + magnitude
            + ")] | CASE WHEN left(cdKey,1)='1' THEN [" + words
            + "] ELSE [cdWord IN [" + words + "] | -cdWord] END])])";
    }

    private sealed class DecimalAggregateCompiler
    {
        private int sequence;
        private string Name() => "cqldec" + sequence++;
        private string Let(string value, Func<string, string> body)
        {
            var name = Name();
            return "head([" + name + " IN [" + value + "] | " + body(name) + "])";
        }
        private string Reduce(string input, string initial, Func<string, string, string> body)
        {
            var state = Name(); var item = Name();
            return "reduce(" + state + "=" + initial + "," + item + " IN " + input + " | " + body(state, item) + ")";
        }
        private static string Choose(string test, string yes, string no)
            => "CASE WHEN " + test + " THEN " + yes + " ELSE " + no + " END";
        private static string Words(BigInteger value)
        {
            var words = new long[8];
            for (var index = 7; index >= 0; index--)
            {
                value = BigInteger.DivRem(value, 1000000000, out var word);
                words[index] = (long)word;
            }
            return "[" + string.Join(",", words) + "]";
        }
        private static string Greater(string first, string second)
            => "(" + string.Join(" OR ", Enumerable.Range(0, 8).Select(index => "("
                + string.Concat(Enumerable.Range(0, index).Select(prior => first + "[" + prior + "]=" + second + "[" + prior + "] AND "))
                + first + "[" + index + "]>" + second + "[" + index + "])")) + ")";

        private string Normalize(string parts)
            => Reduce("reverse(" + parts + ")", "{carry:0,digits:[]}", (state, word) =>
                Let(word + "+" + state + ".carry", value => Let(value + "%1000000000", raw =>
                Let(Choose(raw + "<0", raw + "+1000000000", raw), digit =>
                    "{carry:(" + value + "-" + digit + ")/1000000000,digits:[" + digit + "]+" + state + ".digits}"))));

        private string Divide(string words, string denominator)
            => Reduce(words, "{q:[],r:0}", (state, word) =>
                Let(state + ".r*1000000000+" + word, value =>
                    "{q:" + state + ".q+[" + value + "/" + denominator + "],r:" + value + "%" + denominator + "}"));

        private string Increment(string words)
            => Let(Reduce("reverse(" + words + ")", "{digits:[],carry:1}", (state, word) =>
                Let(word + "+" + state + ".carry", value =>
                    "{digits:[" + value + "%1000000000]+" + state + ".digits,carry:" + value + "/1000000000}")), result => result + ".digits");

        private string Round(string quotient, string remainder, string denominator)
            => Let(Words((BigInteger.One << 96) - 1), maximum =>
                Reduce("range(0,28)", "{q:" + quotient + ",last:0,sticky:" + remainder + "<>0,done:false,chosen:null}",
                    (state, removed) => Choose(state + ".done", state,
                        Let(Choose(removed + "=0",
                            "(" + remainder + "*2>" + denominator + " OR (" + remainder + "*2=" + denominator + " AND " + state + ".q[7]%2=1))",
                            "(" + state + ".last>5 OR (" + state + ".last=5 AND (" + state + ".sticky OR " + state + ".q[7]%2=1)))"), increment =>
                        Let(Choose(increment, Increment(state + ".q"), state + ".q"), rounded =>
                            Choose(Greater(rounded, maximum),
                                Let(Divide(state + ".q", "10"), divided =>
                                    "{q:" + divided + ".q,last:" + divided + ".r,sticky:(" + state + ".sticky OR " + state + ".last<>0),done:false,chosen:null}"),
                                "{q:" + state + ".q,last:" + state + ".last,sticky:" + state + ".sticky,done:true,chosen:{digits:" + rounded + ",scale:28-" + removed + "}}"))))));

        private string Encode(string chosen, string negative)
        {
            const string zeros = "000000000000000000000000000000000000000000000000000000000";
            var coefficient = string.Join("+", Enumerable.Range(0, 8).Select(index =>
                "substring(toString(1000000000+" + chosen + ".digits[" + index + "]),1,9)"));
            return Let("'" + zeros + "'+" + coefficient + "+substring('" + zeros + "',0,28-" + chosen + ".scale)", padded =>
                Let("right(" + padded + ",57)", digits =>
                    Choose(negative + " AND " + digits + "<>'" + zeros + "'", "'0'+"
                        + Reduce("range(0,56)", "''", (text, index) => text
                            + "+substring('9876543210',toInteger(substring(" + digits + "," + index + ",1)),1)"), "'1'+" + digits)));
        }

        internal string Compile(string parts, string count, bool average)
            => Let(parts, input => Let(count, countValue =>
                Choose(countValue + (average ? ">0" : ">=0") + " AND " + countValue + "<=2147483647",
                    Let(Normalize(input), signed => Let(signed + ".carry<0", negative =>
                    Let(Choose(negative, Normalize("[cdWord IN " + input + " | -cdWord]"), signed), magnitude =>
                    Let("[" + magnitude + ".carry]+" + magnitude + ".digits", words =>
                    Let(Words(((BigInteger.One << 96) - 1) * BigInteger.Pow(10, 28)), maximum =>
                        Choose(average ? "false" : Greater(words, maximum), "null",
                            Let(average ? countValue : "1", denominator =>
                            Let(Divide(words, denominator), divided =>
                            Let(Round(divided + ".q", divided + ".r", denominator), result =>
                                Choose(result + ".done", Encode(result + ".chosen", negative), "null")))))))))), "null")));
    }
}
