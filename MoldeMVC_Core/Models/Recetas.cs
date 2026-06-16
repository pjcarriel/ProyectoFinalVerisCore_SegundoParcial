using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

public partial class Recetas
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("consultaId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConsultaId { get; set; } = null!;

    [BsonElement("medicamentoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MedicamentoId { get; set; } = null!;

    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    [BsonIgnore]
    public virtual Consultas? ConsultaNavigation { get; set; }

    [BsonIgnore]
    public virtual Medicamentos? MedicamentoNavigation { get; set; }
}
