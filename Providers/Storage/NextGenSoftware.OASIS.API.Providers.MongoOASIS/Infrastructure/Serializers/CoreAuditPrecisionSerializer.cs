using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

// Core children keep their existing class maps, discriminators and native date
// fields. The precision suffix is part of the Mongo storage representation only.
internal sealed class CoreAuditPrecisionSerializer<T> : SerializerBase<T>, IBsonDocumentSerializer,
    IBsonIdProvider, IBsonPolymorphicSerializer where T : class, IAuditBase
{
    private readonly BsonClassMapSerializer<T> mapped = new(BsonClassMap.LookupClassMap(typeof(T)));
    private static readonly string[] Dates = { nameof(IAuditBase.CreatedDate), nameof(IAuditBase.ModifiedDate), nameof(IAuditBase.DeletedDate) };
    public bool IsDiscriminatorCompatibleWithObjectSerializer => mapped.IsDiscriminatorCompatibleWithObjectSerializer;
    public bool TryGetMemberSerializationInfo(string name, out BsonSerializationInfo info) => mapped.TryGetMemberSerializationInfo(name, out info);
    public bool GetDocumentId(object document, out object id, out Type type, out IIdGenerator generator) => mapped.GetDocumentId(document, out id, out type, out generator);
    public void SetDocumentId(object document, object id) => mapped.SetDocumentId(document, id);

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        if (value == null) { context.Writer.WriteNull(); return; }
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
            mapped.Serialize(BsonSerializationContext.CreateRoot(writer), args, value);
        foreach (var name in Dates)
        {
            if (!mapped.TryGetMemberSerializationInfo(name, out var info))
                throw new BsonSerializationException("Missing mapped audit member " + name + ".");
            var date = name == nameof(IAuditBase.CreatedDate) ? value.CreatedDate
                : name == nameof(IAuditBase.ModifiedDate) ? value.ModifiedDate : value.DeletedDate;
            var field = info.ElementName + "ExactUtcTicks";
            var ticks = MongoAuditDatePrecision.ExactTicks(date);
            if (ticks.HasValue) document[field] = ticks.Value;
            else document.Remove(field);
        }
        if (value is IHolonBase record)
        {
            var precision = MongoMetadataPrecision.Capture(record.MetaData);
            if (precision != null) document[MongoMetadataPrecision.Field] = precision;
            else document.Remove(MongoMetadataPrecision.Field);
        }
        BsonDocumentSerializer.Instance.Serialize(context, document);
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Null) { context.Reader.ReadNull(); return null; }
        var document = BsonDocumentSerializer.Instance.Deserialize(context);
        T value;
        using (var reader = new BsonDocumentReader(document))
            value = mapped.Deserialize(BsonDeserializationContext.CreateRoot(reader), args);
        foreach (var name in Dates)
        {
            if (!mapped.TryGetMemberSerializationInfo(name, out var info))
                throw new BsonSerializationException("Missing mapped audit member " + name + ".");
            if (!document.TryGetValue(info.ElementName + "ExactUtcTicks", out var ticks) || ticks.IsBsonNull) continue;
            if (!ticks.IsInt64) throw new FormatException("Exact audit ticks must be a BSON Int64.");
            var present = document.TryGetValue(info.ElementName, out var projected);
            if (present && !projected.IsBsonDateTime) throw new FormatException("Audit projection must be a BSON date.");
            var restored = MongoAuditDatePrecision.Restore(present ? projected.ToUniversalTime() : default,
                present, ticks.AsInt64, name);
            if (name == nameof(IAuditBase.CreatedDate)) value.CreatedDate = restored;
            else if (name == nameof(IAuditBase.ModifiedDate)) value.ModifiedDate = restored;
            else value.DeletedDate = restored;
        }
        if (value is IHolonBase record && document.TryGetValue(MongoMetadataPrecision.Field, out var precision) && !precision.IsBsonNull)
        {
            if (!precision.IsBsonArray) throw new FormatException("Metadata precision must be a BSON array.");
            MongoMetadataPrecision.Restore(record.MetaData, precision.AsBsonArray);
        }
        return value;
    }
}

internal sealed class CoreAuditPrecisionSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer GetSerializer(Type type)
        => type.IsClass && typeof(IAuditBase).IsAssignableFrom(type)
            && type.Namespace?.StartsWith("NextGenSoftware.OASIS.API.Core.", StringComparison.Ordinal) == true
            ? (IBsonSerializer)Activator.CreateInstance(typeof(CoreAuditPrecisionSerializer<>).MakeGenericType(type)) : null;
}
