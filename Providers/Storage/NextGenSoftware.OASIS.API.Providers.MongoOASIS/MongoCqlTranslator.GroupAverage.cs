using MongoDB.Bson;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests")]

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // Decimal128 holds every intermediate integer exactly (at most30 digits).
    // Never use its rounded fractional quotient as the published mean. Extract
    // bounded digits, enforce Decimal's96-bit coefficient and round to even.
    internal static BsonValue GroupedAverageValue(int index)
    {
        BsonValue D(decimal value) => Literal(new BsonDecimal128(new Decimal128(value)));
        BsonValue Truncate(BsonValue value) => new BsonDocument("$trunc", new BsonArray { value, Literal(0) });
        BsonValue Let(string name, BsonValue value, BsonValue body) => new BsonDocument("$let", new BsonDocument
        { { "vars", new BsonDocument(name, value) }, { "in", body } });
        var zero = D(0); var one = D(1); var ten = D(10); var maximum = D(decimal.MaxValue);
        var count = new BsonDocument("$toDecimal", "$a" + index + "present");
        var total = GroupedSumValue(index);
        BsonValue Round(string coefficient, string remainder) => Op("$add", coefficient,
            Choose(Op("$or", Op("$gt", Op("$multiply", remainder, D(2)), "$$denominator"),
                Op("$and", Op("$eq", Op("$multiply", remainder, D(2)), "$$denominator"),
                    Op("$ne", Op("$mod", coefficient, D(2)), zero))), one, zero));

        var initial = new BsonDocument
        {
            { "c", "$$quotient" }, { "r", "$$remainder" }, { "s", Literal(0) },
            { "pc", "$$quotient" }, { "pr", "$$remainder" }, { "ps", Literal(0) },
            { "done", Op("$eq", "$$remainder", zero) }
        };
        var stop = new BsonDocument
        {
            { "c", "$$value.c" }, { "r", "$$value.r" }, { "s", "$$value.s" },
            { "pc", "$$value.pc" }, { "pr", "$$value.pr" }, { "ps", "$$value.ps" }, { "done", Literal(true) }
        };
        var accepted = Let("nextRemainder", Op("$mod", "$$expanded", "$$denominator"), new BsonDocument
        {
            { "c", "$$candidate" }, { "r", "$$nextRemainder" }, { "s", Op("$add", "$$value.s", Literal(1)) },
            { "pc", "$$value.c" }, { "pr", "$$value.r" }, { "ps", "$$value.s" },
            { "done", Op("$eq", "$$nextRemainder", zero) }
        });
        var step = Choose("$$value.done", "$$value", Let("expanded", Op("$multiply", "$$value.r", ten),
            Let("candidate", Op("$add", Op("$multiply", "$$value.c", ten),
                Truncate(Op("$divide", "$$expanded", "$$denominator"))),
                Choose(Op("$gt", "$$candidate", maximum), stop, accepted))));
        var digits = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Op("$range", Literal(0), Literal(28)) }, { "initialValue", initial }, { "in", step }
        });
        var powers = new BsonArray();
        decimal power = 1;
        for (var scale = 0; scale <= 28; scale++)
        {
            powers.Add(new BsonDecimal128(new Decimal128(power)));
            if (scale != 28) power *= 10;
        }
        var rounded = Round("$$digits.c", "$$digits.r");
        // Rounding MaxCoefficient+fraction may require one fewer scale digit.
        // Re-round the prior state rather than rounding an already rounded value.
        var normalized = Let("rounded", rounded, Let("chosen", Choose(Op("$gt", "$$rounded", maximum),
            new BsonDocument { { "c", Round("$$digits.pc", "$$digits.pr") }, { "s", "$$digits.ps" } },
            new BsonDocument { { "c", "$$rounded" }, { "s", "$$digits.s" } }),
            Op("$multiply", Choose(Op("$lt", "$$total", zero), D(-1), one),
                Op("$divide", "$$chosen.c", Op("$arrayElemAt", Literal(powers), "$$chosen.s")))));

        // Count<=2^31-1 and |total|<2^94 imply an integral quotient with at
        // most19 digits. Its distance from a non-equal integer is >=1/count,
        // far greater than Decimal128's division error at this magnitude.
        // Truncation therefore extracts the exact integer quotient. All later
        // quotients are single digits with the same stronger error margin.
        return Choose(Op("$gt", count, zero), Let("denominator", count, Let("total", total,
            Let("magnitude", new BsonDocument("$abs", "$$total"),
                Let("quotient", Truncate(Op("$divide", "$$magnitude", "$$denominator")),
                    Let("remainder", Op("$subtract", "$$magnitude", Op("$multiply", "$$quotient", "$$denominator")),
                        Let("digits", digits, normalized)))))), Literal(BsonNull.Value));
    }
}
