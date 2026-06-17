using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

[BsonIgnoreExtraElements]
public partial class Pacientes
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("cedula")]
    public int Cedula { get; set; }

    [BsonElement("edad")]
    public int Edad { get; set; }

    [BsonElement("genero")]
    public string Genero { get; set; } = null!;

    [BsonElement("estatura")]
    public int Estatura { get; set; }

    [BsonElement("peso")]
    public double Peso { get; set; }

    [BsonElement("foto")]
    public string Foto { get; set; } = null!;

    [BsonIgnore]
    public virtual ICollection<Consultas> Consulta { get; set; } = new List<Consultas>();
}
