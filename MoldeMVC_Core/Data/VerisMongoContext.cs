using MoldeMVC_Core.Models;
using MongoDB.Driver;

namespace MoldeMVC_Core.Data
{
    public class VerisMongoContext
    {
        public IMongoCollection<Pacientes> Pacientes { get; }
        public IMongoCollection<Medicos> Medicos { get; }
        public IMongoCollection<Medicamentos> Medicamentos { get; }
        public IMongoCollection<Especialidades> Especialidades { get; }
        public IMongoCollection<Consultas> Consultas { get; }
        public IMongoCollection<Recetas> Recetas { get; }

        public VerisMongoContext(IMongoDatabase database)
        {
            Pacientes = database.GetCollection<Pacientes>("pacientes");
            Medicos = database.GetCollection<Medicos>("medicos");
            Medicamentos = database.GetCollection<Medicamentos>("medicamentos");
            Especialidades = database.GetCollection<Especialidades>("especialidades");
            Consultas = database.GetCollection<Consultas>("consultas");
            Recetas = database.GetCollection<Recetas>("recetas");
        }
    }
}
