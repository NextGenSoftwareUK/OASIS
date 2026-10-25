using System;
using System.Collections.Generic;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// Compare original JSON decimal text with exact binary32 midpoints. A Double
// parse before rounding would change values on opposite sides of a midpoint.
internal static class NeoCqlSingle
{
    internal static (string Value, string Guard) Operand(string kind, string value, Func<object, string> bind)
    {
        string Let(string name, string expression, string body)
            => "head([" + name + " IN [" + expression + "] | " + body + "])";
        string When(string condition, string body)
            => "CASE WHEN " + condition + " THEN " + body + " ELSE null END";

        var factors = new List<long[]>();
        for (var exponent = -150; exponent <= 103; exponent++)
        {
            var power = BigInteger.Pow(exponent < 0 ? 5 : 2, Math.Abs(exponent));
            var limbs = new List<long>();
            do { limbs.Add((long)(power % 100000000)); power /= 100000000; } while (power != 0);
            factors.Add(limbs.ToArray());
        }
        var factorParameter = bind(factors.ToArray());
        var powers = new double[255];
        for (var exponent = 0; exponent < powers.Length; exponent++)
            powers[exponent] = Math.Pow(2, exponent == 0 ? -149 : exponent - 150);
        var powerParameter = bind(powers);
        var zeroes = bind(new string('0', 4096));
        var grammar = bind(@"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z");

        // Each factor limb times the 25-bit coefficient plus carry stays below
        // 3.4e15. Integer division/remainders preserve every decimal digit.
        var multiply = "reduce(csProduct={carry:0,limbs:[]},csLimb IN " + factorParameter
            + "[csExponent+150] | " + Let("csTerm", "csLimb*csCoefficient+csProduct.carry",
                "{carry:csTerm/100000000,limbs:csProduct.limbs+[csTerm%100000000]}") + ")";
        var digits = "reduce(csText='',csLimb IN reverse(csProduct.limbs+[csProduct.carry]) | "
            + "csText+right('00000000'+toString(csLimb),8))";
        var boundary = Let("csProduct", multiply, Let("csPadded", digits,
            Let("csFirst", "head([csI IN range(0,size(csPadded)-1) WHERE substring(csPadded,csI,1)<>'0'])",
                "{digits:substring(csPadded,csFirst),scale:CASE WHEN csExponent<0 THEN csExponent ELSE 0 END}")));
        var midpoint = Let("csBinaryExponent", "csMid/8388608",
            Let("csExponent", "CASE WHEN csBinaryExponent=0 THEN -150 ELSE csBinaryExponent-151 END",
                Let("csCoefficient", "2*(csMid%8388608+CASE WHEN csBinaryExponent=0 THEN 0 ELSE 8388608 END)+1", boundary)));

        // Scientific magnitude first, then zero-padded significant digits. Both
        // sides retain their complete decimal expansion, including midpoint tails.
        var compare = Let("csBoundary", midpoint,
            Let("csBoundaryOrder", "size(csBoundary.digits)+csBoundary.scale",
                Let("csWidth", "CASE WHEN size(csDigits)>size(csBoundary.digits) THEN size(csDigits) ELSE size(csBoundary.digits) END",
                    Let("csLeft", "substring(csDigits+" + zeroes + ",0,csWidth)",
                        Let("csRight", "substring(csBoundary.digits+" + zeroes + ",0,csWidth)",
                            "csOrder>csBoundaryOrder OR csOrder=csBoundaryOrder AND "
                            + "(csLeft>csRight OR csLeft=csRight AND csMid%2=1)")))));
        var step = "CASE WHEN csRange.low<csRange.high THEN "
            + Let("csMid", "(csRange.low+csRange.high)/2",
                "CASE WHEN " + compare + " THEN {low:csMid+1,high:csRange.high} "
                + "ELSE {low:csRange.low,high:csMid} END") + " ELSE csRange END";
        var search = "reduce(csRange={low:0,high:2139095040},csIteration IN range(0,30) | " + step + ")";
        var decoded = Let("csBinaryExponent", "csFinished.low/8388608",
            "(csFinished.low%8388608+CASE WHEN csBinaryExponent=0 THEN 0 ELSE 8388608 END)*"
            + powerParameter + "[csBinaryExponent]");
        var rounded = Let("csFinished", search,
            When("csFinished.low>0 AND csFinished.low<2139095040",
                Let("csNumber", decoded, "CASE WHEN left(csRaw,1)='-' THEN -csNumber ELSE csNumber END")));
        var nonzero = Let("csDigits", "substring(csAllDigits,head(csNonzero))",
            Let("csPower", "CASE WHEN size(csParts)=1 THEN 0 ELSE toInteger(csParts[1]) END",
                When("csPower>=-8192 AND csPower<=8192",
                    Let("csOrder", "size(csDigits)+csPower-csFraction", When("csOrder>=-200 AND csOrder<=100", rounded)))));
        var parsed = Let("csParts", "split(toLower(csUnsigned),'e')",
            Let("csMantissa", "csParts[0]",
                Let("csAllDigits", "replace(csMantissa,'.','')",
                    Let("csFraction", "CASE WHEN size(split(csMantissa,'.'))=1 THEN 0 ELSE size(split(csMantissa,'.')[1]) END",
                        Let("csNonzero", "[csI IN range(0,size(csAllDigits)-1) WHERE substring(csAllDigits,csI,1)<>'0']",
                            "CASE WHEN size(csNonzero)=0 THEN 0.0 ELSE " + nonzero + " END")))));
        var raw = "CASE WHEN " + kind + "='integer' THEN toString(" + value + ") WHEN "
            + kind + "='number' THEN " + value + " ELSE null END";
        var normalized = Let("csRaw", raw,
            When("size(csRaw)<=4096 AND csRaw =~ " + grammar,
                Let("csUnsigned", "CASE WHEN left(csRaw,1)='-' THEN substring(csRaw,1) ELSE csRaw END", parsed)));
        return (normalized, "(" + normalized + ") IS NOT NULL");
    }
}
