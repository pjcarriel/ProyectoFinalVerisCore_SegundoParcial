using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

public partial class Medicos
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("especialidadId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EspecialidadId { get; set; } = null!;

    [BsonElement("foto")]
    public string Foto { get; set; } = null!;

    [BsonElement("idUsuario")]
    public string? IdUsuario { get; set; }

    [BsonIgnore]
    public virtual ICollection<Consultas> Consulta { get; set; } = new List<Consultas>();

    [BsonIgnore]
    public virtual Especialidades? EspecialidadNavigation { get; set; }
}
