using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<MoldeMVC_Core.Models.Pacientes> Pacientes { get; set; } = default!;
        public DbSet<MoldeMVC_Core.Models.Medicos> Medicos { get; set; } = default!;
        public DbSet<MoldeMVC_Core.Models.Recetas> Recetas { get; set; } = default!;
        public DbSet<MoldeMVC_Core.Models.Medicamentos> Medicamentos { get; set; } = default!;
        public DbSet<MoldeMVC_Core.Models.Especialidades> Especialidades { get; set; } = default!;
        public DbSet<MoldeMVC_Core.Models.Consultas> Consultas { get; set; } = default!;
    }
}
