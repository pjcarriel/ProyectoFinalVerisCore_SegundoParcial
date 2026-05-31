using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MoldeMVC_Core.Models
{
    public class Especialidades
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = default!;

        public string descripcion { get; set; } = default!;

        public string dias { get; set; } = default!;

        public string franjaHI { get; set; } = default!;

        public string franjaHF { get; set; } = default!;
    }
}