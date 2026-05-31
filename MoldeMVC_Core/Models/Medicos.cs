using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoldeMVC_Core.Models
{
    public class Medicos
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = default!;

        public string nombre { get; set; } = default!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string especialidadId { get; set; } = default!;

        public string foto { get; set; } = default!;
    }
}