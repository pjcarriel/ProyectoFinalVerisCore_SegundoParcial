using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoldeMVC_Core.Models;

public partial class Medicamentos
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonIgnore]
    public string IdStr => Id.ToString();

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("tipo")]
    public string Tipo { get; set; } = null!;

    [BsonIgnore]
    public virtual ICollection<Recetas> Receta { get; set; } = new List<Recetas>();
}
