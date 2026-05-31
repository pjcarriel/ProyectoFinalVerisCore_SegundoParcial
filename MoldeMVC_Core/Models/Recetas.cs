using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoldeMVC_Core.Models
{
    public class Recetas
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = default!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string consultaId { get; set; } = default!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string medicamentoId { get; set; } = default!;

        public int cantidad { get; set; }
    }
}