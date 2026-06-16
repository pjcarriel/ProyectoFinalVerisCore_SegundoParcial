using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Medico,SuperAdmin,Paciente")]
    public class ConsultasController : Controller
    {
        private readonly MongoDbContext _mongo;

        public ConsultasController(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> Index()
        {
            List<Consultas> consultas;

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var paciente = await _mongo.Pacientes
                    .Find(p => p.IdUsuario == objUser!.Id)
                    .FirstOrDefaultAsync();

                if (paciente == null) return View(new List<Consultas>());

                var pacienteIdStr = paciente.Id.ToString();
                consultas = await _mongo.Consultas
                    .Find(c => c.PacienteId == pacienteIdStr)
                    .ToListAsync();
            }
            else
            {
                consultas = await _mongo.Consultas.Find(_ => true).ToListAsync();
            }

            await PopularNavegacion(consultas);
            return View(consultas);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return NotFound();

            consulta.MedicoNavigation   = await _mongo.Medicos.Find(m => m.Id == ObjectId.Parse(consulta.MedicoId)).FirstOrDefaultAsync();
            consulta.PacienteNavigation = await _mongo.Pacientes.Find(p => p.Id == ObjectId.Parse(consulta.PacienteId)).FirstOrDefaultAsync();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (consulta.PacienteNavigation?.IdUsuario != objUser?.Id)
                    return Forbid();
            }

            ViewBag.MedicoNombre   = consulta.MedicoNavigation?.Nombre ?? "";
            ViewBag.PacienteNombre = consulta.PacienteNavigation?.Nombre ?? "";
            return View(consulta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await RecargarSelects();
            return View();
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MedicoId,PacienteId,FechaConsulta,Hi,Hf,Diagnostico")] Consultas consulta)
        {
            ModelState.Remove("MedicoNavigation");
            ModelState.Remove("PacienteNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consulta.MedicoId, consulta.PacienteId);
                return View(consulta);
            }

            try
            {
                await _mongo.Consultas.InsertOneAsync(consulta);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consulta.MedicoId, consulta.PacienteId);
                return View(consulta);
            }
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return NotFound();

            consulta.MedicoNavigation = await _mongo.Medicos
                .Find(m => m.Id == ObjectId.Parse(consulta.MedicoId))
                .FirstOrDefaultAsync();

            var especialidades = await _mongo.Especialidades.Find(_ => true).ToListAsync();
            var pacientes = await _mongo.Pacientes.Find(_ => true).SortBy(p => p.Nombre).ToListAsync();

            ViewBag.Especialidades       = especialidades;
            ViewBag.PacientesList        = pacientes;
            ViewBag.MedicoEspecialidadId = consulta.MedicoNavigation?.EspecialidadId ?? "";
            ViewBag.MedicoNombre         = consulta.MedicoNavigation?.Nombre ?? "";
            return View(consulta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MedicoId,PacienteId,FechaConsulta,Hi,Hf,Diagnostico")] Consultas consulta)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            ModelState.Remove("MedicoNavigation");
            ModelState.Remove("PacienteNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consulta.MedicoId, consulta.PacienteId);
                return View(consulta);
            }

            try
            {
                consulta.Id = oid;
                await _mongo.Consultas.ReplaceOneAsync(c => c.Id == oid, consulta);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consulta.MedicoId, consulta.PacienteId);
                return View(consulta);
            }
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Atender(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return NotFound();

            consulta.MedicoNavigation   = await _mongo.Medicos.Find(m => m.Id == ObjectId.Parse(consulta.MedicoId)).FirstOrDefaultAsync();
            consulta.PacienteNavigation = await _mongo.Pacientes.Find(p => p.Id == ObjectId.Parse(consulta.PacienteId)).FirstOrDefaultAsync();

            ViewBag.MedicoNombre   = consulta.MedicoNavigation?.Nombre ?? "—";
            ViewBag.PacienteNombre = consulta.PacienteNavigation?.Nombre ?? "—";
            ViewBag.MedicoFoto     = consulta.MedicoNavigation?.Foto ?? "";
            ViewBag.PacienteFoto   = consulta.PacienteNavigation?.Foto ?? "";
            return View(consulta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atender(string id, string diagnostico)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                ModelState.AddModelError("diagnostico", "El diagnóstico no puede estar vacío.");
                var c = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
                c!.MedicoNavigation   = await _mongo.Medicos.Find(m => m.Id == ObjectId.Parse(c.MedicoId)).FirstOrDefaultAsync();
                c.PacienteNavigation  = await _mongo.Pacientes.Find(p => p.Id == ObjectId.Parse(c.PacienteId)).FirstOrDefaultAsync();
                ViewBag.MedicoNombre   = c.MedicoNavigation?.Nombre ?? "—";
                ViewBag.PacienteNombre = c.PacienteNavigation?.Nombre ?? "—";
                ViewBag.MedicoFoto     = c.MedicoNavigation?.Foto ?? "";
                ViewBag.PacienteFoto   = c.PacienteNavigation?.Foto ?? "";
                return View(c);
            }

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return NotFound();

            consulta.Diagnostico = diagnostico.Trim();
            await _mongo.Consultas.ReplaceOneAsync(c => c.Id == oid, consulta);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return NotFound();

            consulta.MedicoNavigation   = await _mongo.Medicos.Find(m => m.Id == ObjectId.Parse(consulta.MedicoId)).FirstOrDefaultAsync();
            consulta.PacienteNavigation = await _mongo.Pacientes.Find(p => p.Id == ObjectId.Parse(consulta.PacienteId)).FirstOrDefaultAsync();
            return View(consulta);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            await _mongo.Consultas.DeleteOneAsync(c => c.Id == oid);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopularNavegacion(List<Consultas> consultas)
        {
            var medicoIds   = consultas.Select(c => c.MedicoId).Distinct().Select(ObjectId.Parse).ToList();
            var pacienteIds = consultas.Select(c => c.PacienteId).Distinct().Select(ObjectId.Parse).ToList();

            var medicos   = await _mongo.Medicos.Find(m => medicoIds.Contains(m.Id)).ToListAsync();
            var pacientes = await _mongo.Pacientes.Find(p => pacienteIds.Contains(p.Id)).ToListAsync();

            var medDict = medicos.ToDictionary(m => m.Id.ToString());
            var pacDict = pacientes.ToDictionary(p => p.Id.ToString());

            foreach (var c in consultas)
            {
                c.MedicoNavigation   = medDict.GetValueOrDefault(c.MedicoId);
                c.PacienteNavigation = pacDict.GetValueOrDefault(c.PacienteId);
            }
        }

        private async Task RecargarSelects(string? selectedMedicoId = null, string? selectedPacienteId = null)
        {
            var medicos   = await _mongo.Medicos.Find(_ => true).ToListAsync();
            var pacientes = await _mongo.Pacientes.Find(_ => true).ToListAsync();

            ViewBag.Medicos   = new SelectList(medicos,   "IdStr", "Nombre", selectedMedicoId);
            ViewBag.Pacientes = new SelectList(pacientes, "IdStr", "Nombre", selectedPacienteId);
        }
    }
}
