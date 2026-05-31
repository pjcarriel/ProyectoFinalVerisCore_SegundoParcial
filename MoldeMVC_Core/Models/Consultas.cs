using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoldeMVC_Core.Models
{
    public class Consultas
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = default!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string medicoId { get; set; } = default!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string pacienteId { get; set; } = default!;

        public DateTime fechaConsulta { get; set; }

        public string hi { get; set; } = default!;

        public string hf { get; set; } = default!;

        public string diagnostico { get; set; } = default!;
    }
}