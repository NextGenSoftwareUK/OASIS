using System;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static string GroupedAverageKey(string high, string low, string count)
        => new AverageKeyCompiler().Compile(high, low, count);

    // All Cypher arithmetic is Int64. Four base10^9 limbs hold the96-bit
    // coefficient; remainder*base stays below2^31*10^9. No Float conversion.
    private sealed class AverageKeyCompiler
    {
        private int sequence;
        private string Name() => "cqlavg" + sequence++;
        private string Let(string expression, Func<string, string> body)
        {
            var name = Name();
            return "head([" + name + " IN [" + expression + "] | " + body(name) + "])";
        }
        private string Reduce(string initial, string input, Func<string, string, string> body)
        {
            var state = Name(); var item = Name();
            return "reduce(" + state + "=" + initial + "," + item + " IN " + input + " | " + body(state, item) + ")";
        }
        private static string Greater(string first, string second)
            => "(" + string.Join(" OR ", Enumerable.Range(0, 4).Select(index => "("
                + string.Concat(Enumerable.Range(0, index).Select(prior => first + "[" + prior + "]=" + second + "[" + prior + "] AND "))
                + first + "[" + index + "]>" + second + "[" + index + "])")) + ")";

        private string MultiplyAdd(string digits, string multiplier, string carry)
            => Let(Reduce("{digits:[],carry:" + carry + "}", "[3,2,1,0]", (state, index) =>
                Let(digits + "[" + index + "]*" + multiplier + "+" + state + ".carry", value =>
                    "{digits:[" + value + "%1000000000]+" + state + ".digits,carry:" + value + "/1000000000}")), result => result + ".digits");

        private string Round(string digits, string remainder, string count)
            => MultiplyAdd(digits, "1", "CASE WHEN " + remainder + "*2>" + count + " OR ("
                + remainder + "*2=" + count + " AND " + digits + "[3]%2=1) THEN 1 ELSE 0 END");

        private static string State(string c, string r, string s, string pc, string pr, string ps, string done)
            => "{c:" + c + ",r:" + r + ",s:" + s + ",pc:" + pc + ",pr:" + pr + ",ps:" + ps + ",done:" + done + "}";

        private string Extend(string state, string count, string maximum)
            => "CASE WHEN " + state + ".done THEN " + state + " ELSE "
                + Let(state + ".r*10", expanded => Let(MultiplyAdd(state + ".c", "10", expanded + "/" + count), candidate =>
                    "CASE WHEN " + Greater(candidate, maximum) + " THEN "
                    + State(state + ".c", state + ".r", state + ".s", state + ".pc", state + ".pr", state + ".ps", "true")
                    + " ELSE " + State(candidate, expanded + "%" + count, state + ".s+1", state + ".c", state + ".r", state + ".s",
                        expanded + "%" + count + "=0") + " END")) + " END";

        private string Encode(string chosen, string negative)
        {
            const string zeros = "000000000000000000000000000000000000000000000000000000000";
            var coefficient = string.Join("+", Enumerable.Range(0, 4).Select(index =>
                "substring(toString(1000000000+" + chosen + ".c[" + index + "]),1,9)"));
            return Let("'" + zeros + "'+" + coefficient + "+substring('" + zeros + "',0,28-" + chosen + ".s)", padded =>
                Let("substring(" + padded + ",size(" + padded + ")-57,57)", digits =>
                    "CASE WHEN " + negative + " THEN '0'+"
                    + Reduce("''", "range(0,56)", (text, index) => text
                        + "+substring('9876543210',toInteger(substring(" + digits + "," + index + ",1)),1)")
                    + " ELSE '1'+" + digits + " END"));
        }

        internal string Compile(string high, string low, string count)
        {
            var parts = GroupedIntegralParts(high, low);
            return "CASE WHEN " + count + ">0 THEN " + Let(count, denominator =>
                Let("[" + parts.High + "," + parts.Low + "]", floor =>
                Let(floor + "[0]<0", negative =>
                Let("CASE WHEN " + negative + " THEN -" + floor + "[0]-CASE WHEN " + floor + "[1]>0 THEN 1 ELSE 0 END ELSE " + floor + "[0] END", upper =>
                Let("CASE WHEN " + negative + " AND " + floor + "[1]>0 THEN 4294967296-" + floor + "[1] ELSE " + floor + "[1] END", lower =>
                Let(Reduce("[0,0,0,0]", "[" + upper + "/4294967296," + upper + "%4294967296," + lower + "]",
                    (digits, word) => MultiplyAdd(digits, "4294967296", word)), magnitude =>
                Let(Reduce("{digits:[],r:0}", magnitude, (state, digit) =>
                    Let(state + ".r*1000000000+" + digit, expanded =>
                        "{digits:" + state + ".digits+[" + expanded + "/" + denominator + "],r:" + expanded + "%" + denominator + "}")), quotient =>
                Let("[79,228162514,264337593,543950335]", maximum =>
                Let(Reduce(State(quotient + ".digits", quotient + ".r", "0", quotient + ".digits", quotient + ".r", "0", quotient + ".r=0"),
                    "range(1,28)", (state, _) => Extend(state, denominator, maximum)), result =>
                Let(Round(result + ".c", result + ".r", denominator), rounded =>
                Let("CASE WHEN " + Greater(rounded, maximum) + " THEN {c:" + Round(result + ".pc", result + ".pr", denominator)
                    + ",s:" + result + ".ps} ELSE {c:" + rounded + ",s:" + result + ".s} END", chosen =>
                    Encode(chosen, negative)))))))))))) + " ELSE null END";
        }
    }
}
