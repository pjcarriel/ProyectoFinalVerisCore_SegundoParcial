using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

public partial class Especialidades
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("descripcion")]
    public string Descripcion { get; set; } = null!;

    [BsonElement("dias")]
    public string Dias { get; set; } = null!;

    [BsonElement("franjaHI")]
    public TimeOnly FranjaHi { get; set; }

    [BsonElement("franjaHF")]
    public TimeOnly FranjaHf { get; set; }

    [BsonIgnore]
    public virtual ICollection<Medicos> Medicos { get; set; } = new List<Medicos>();
}
