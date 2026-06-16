using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

public partial class Consultas
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("medicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MedicoId { get; set; } = null!;

    [BsonElement("pacienteId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PacienteId { get; set; } = null!;

    [BsonElement("fechaConsulta")]
    public DateOnly FechaConsulta { get; set; }

    [BsonElement("hi")]
    public TimeOnly Hi { get; set; }

    [BsonElement("hf")]
    public TimeOnly Hf { get; set; }

    [BsonElement("diagnostico")]
    public string Diagnostico { get; set; } = null!;

    [BsonIgnore]
    public virtual Medicos? MedicoNavigation { get; set; }

    [BsonIgnore]
    public virtual Pacientes? PacienteNavigation { get; set; }

    [BsonIgnore]
    public virtual ICollection<Recetas> Receta { get; set; } = new List<Recetas>();
}
