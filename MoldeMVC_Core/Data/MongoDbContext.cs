using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Data;

public class MongoDbContext
{
    private static bool _serializersRegistered;
    private static readonly object _lock = new();

    private readonly IMongoDatabase _db;

    public MongoDbContext(IConfiguration config)
    {
        EnsureSerializers();
        var client = new MongoClient(config.GetConnectionString("MongoConnection"));
        _db = client.GetDatabase("ProyectoVeris_MongoBD");
    }

    private static void EnsureSerializers()
    {
        if (_serializersRegistered) return;
        lock (_lock)
        {
            if (_serializersRegistered) return;
            BsonSerializer.RegisterSerializer(new DateOnlyBsonSerializer());
            BsonSerializer.RegisterSerializer(new TimeOnlyBsonSerializer());
            _serializersRegistered = true;
        }
    }

    public IMongoCollection<Especialidades> Especialidades =>
        _db.GetCollection<Especialidades>("especialidades");

    public IMongoCollection<Medicamentos> Medicamentos =>
        _db.GetCollection<Medicamentos>("medicamentos");

    public IMongoCollection<Medicos> Medicos =>
        _db.GetCollection<Medicos>("medicos");

    public IMongoCollection<Pacientes> Pacientes =>
        _db.GetCollection<Pacientes>("pacientes");

    public IMongoCollection<Consultas> Consultas =>
        _db.GetCollection<Consultas>("consultas");

    public IMongoCollection<Recetas> Recetas =>
        _db.GetCollection<Recetas>("recetas");
}

public class DateOnlyBsonSerializer : SerializerBase<DateOnly>
{
    public override DateOnly Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
    {
        var type = ctx.Reader.GetCurrentBsonType();
        if (type == BsonType.String)
            return DateOnly.Parse(ctx.Reader.ReadString());
        if (type == BsonType.DateTime)
            return DateOnly.FromDateTime(
                DateTimeOffset.FromUnixTimeMilliseconds(ctx.Reader.ReadDateTime()).UtcDateTime);
        throw new BsonSerializationException($"No se puede deserializar DateOnly desde {type}");
    }

    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, DateOnly value)
        => ctx.Writer.WriteDateTime(
            new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeMilliseconds());
}

public class TimeOnlyBsonSerializer : SerializerBase<TimeOnly>
{
    public override TimeOnly Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
    {
        var type = ctx.Reader.GetCurrentBsonType();
        if (type == BsonType.String)
            return TimeOnly.Parse(ctx.Reader.ReadString());
        if (type == BsonType.Int32)
            return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(ctx.Reader.ReadInt32()));
        if (type == BsonType.Int64)
            return TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ctx.Reader.ReadInt64()));
        throw new BsonSerializationException($"No se puede deserializar TimeOnly desde {type}");
    }

    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, TimeOnly value)
        => ctx.Writer.WriteString(value.ToString("HH:mm:ss"));
}
