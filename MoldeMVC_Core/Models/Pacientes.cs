using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoldeMVC_Core.Models
{
    public class Pacientes
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = default!;
        public string nombre { get; set; } = default!;
        public int cedula { get; set; } = default!;
        public int edad { get; set; } = default!;
        public string genero { get; set; } = default!;
        public int estatura { get; set; } = default!;
        public int peso { get; set; } = default!;
        public string foto { get; set; } = default!;

    }
}
