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
    public class RecetasController : Controller
    {
        private readonly MongoDbContext _mongo;

        public RecetasController(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> Index()
        {
            List<Recetas> recetas;

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (!int.TryParse(objUser?.PhoneNumber, out var cedula))
                    return View(new List<Recetas>());
                var paciente = await _mongo.Pacientes
                    .Find(p => p.Cedula == cedula)
                    .FirstOrDefaultAsync();

                if (paciente == null) return View(new List<Recetas>());

                var pacienteIdStr = paciente.Id.ToString();
                var consultasPaciente = await _mongo.Consultas
                    .Find(c => c.PacienteId == pacienteIdStr)
                    .ToListAsync();

                var consultaIds = consultasPaciente.Select(c => c.Id.ToString()).ToList();
                recetas = await _mongo.Recetas
                    .Find(r => consultaIds.Contains(r.ConsultaId))
                    .ToListAsync();

                var consDict = consultasPaciente.ToDictionary(c => c.Id.ToString());
                foreach (var r in recetas)
                {
                    r.ConsultaNavigation = consDict.GetValueOrDefault(r.ConsultaId);
                    if (r.ConsultaNavigation != null)
                        r.ConsultaNavigation.PacienteNavigation = paciente;
                }
            }
            else if (User.IsInRole("Medico"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var medico = await _mongo.Medicos
                    .Find(m => m.Nombre == objUser!.UserName)
                    .FirstOrDefaultAsync();

                if (medico == null) return View(new List<Recetas>());

                var medicoIdStr = medico.Id.ToString();
                var consultasMedico = await _mongo.Consultas
                    .Find(c => c.MedicoId == medicoIdStr)
                    .ToListAsync();

                var consultaIds = consultasMedico.Select(c => c.Id.ToString()).ToList();
                recetas = await _mongo.Recetas
                    .Find(r => consultaIds.Contains(r.ConsultaId))
                    .ToListAsync();

                var consDict = consultasMedico.ToDictionary(c => c.Id.ToString());
                foreach (var r in recetas)
                    r.ConsultaNavigation = consDict.GetValueOrDefault(r.ConsultaId);
            }
            else
            {
                recetas = await _mongo.Recetas.Find(_ => true).ToListAsync();
                await PopularNavegacion(recetas);
            }

            await PopularMedicamentos(recetas);
            return View(recetas);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var receta = await _mongo.Recetas.Find(r => r.Id == oid).FirstOrDefaultAsync();
            if (receta == null) return NotFound();

            receta.ConsultaNavigation    = await _mongo.Consultas.Find(c => c.Id == ObjectId.Parse(receta.ConsultaId)).FirstOrDefaultAsync();
            receta.MedicamentoNavigation = await _mongo.Medicamentos.Find(m => m.Id == ObjectId.Parse(receta.MedicamentoId)).FirstOrDefaultAsync();

            if (receta.ConsultaNavigation != null)
                receta.ConsultaNavigation.PacienteNavigation = await _mongo.Pacientes
                    .Find(p => p.Id == ObjectId.Parse(receta.ConsultaNavigation.PacienteId))
                    .FirstOrDefaultAsync();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (receta.ConsultaNavigation?.PacienteNavigation?.Cedula.ToString() != objUser?.PhoneNumber)
                    return Forbid();
            }

            ViewBag.ConsultaNombre    = receta.ConsultaNavigation?.Diagnostico ?? "";
            ViewBag.MedicamentoNombre = receta.MedicamentoNavigation?.Nombre ?? "";
            return View(receta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await RecargarCreateSelects();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetConsultasPaciente(string pacienteId)
        {
            var consultas = await _mongo.Consultas
                .Find(c => c.PacienteId == pacienteId)
                .SortByDescending(c => c.FechaConsulta)
                .ToListAsync();

            var medicoIds = consultas.Select(c => c.MedicoId).Distinct().Select(ObjectId.Parse).ToList();
            var medicos   = await _mongo.Medicos.Find(m => medicoIds.Contains(m.Id)).ToListAsync();
            var medDict   = medicos.ToDictionary(m => m.Id.ToString());

            var resultado = consultas.Select(c => new
            {
                id          = c.Id.ToString(),
                texto       = $"{c.FechaConsulta:dd/MM/yyyy}  {c.Hi:hh\\:mm}–{c.Hf:hh\\:mm}  |  {medDict.GetValueOrDefault(c.MedicoId)?.Nombre ?? "?"}",
                diagnostico = c.Diagnostico ?? "Pendiente",
                pendiente   = string.IsNullOrWhiteSpace(c.Diagnostico) || c.Diagnostico.Trim().ToLower() == "pendiente"
            });

            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> GetDiagnostico(string consultaId)
        {
            if (!ObjectId.TryParse(consultaId, out var oid))
                return Json(new { diagnostico = "", pendiente = true });

            var consulta = await _mongo.Consultas.Find(c => c.Id == oid).FirstOrDefaultAsync();
            if (consulta == null) return Json(new { diagnostico = "", pendiente = true });

            bool pendiente = string.IsNullOrWhiteSpace(consulta.Diagnostico) ||
                             consulta.Diagnostico.Trim().ToLower() == "pendiente";

            return Json(new { diagnostico = consulta.Diagnostico ?? "Pendiente", pendiente });
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ConsultaId,MedicamentoId,Cantidad")] Recetas receta)
        {
            ModelState.Remove("ConsultaNavigation");
            ModelState.Remove("MedicamentoNavigation");

            if (!string.IsNullOrWhiteSpace(receta.ConsultaId))
            {
                var consulta = await _mongo.Consultas.Find(c => c.Id == ObjectId.Parse(receta.ConsultaId)).FirstOrDefaultAsync();
                if (consulta == null ||
                    string.IsNullOrWhiteSpace(consulta.Diagnostico) ||
                    consulta.Diagnostico.Trim().ToLower() == "pendiente")
                {
                    ModelState.AddModelError("", "La consulta seleccionada aún no tiene diagnóstico. Registre el diagnóstico antes de crear la receta.");
                }
            }

            if (!ModelState.IsValid)
            {
                await RecargarCreateSelects(receta.MedicamentoId);
                return View(receta);
            }

            try
            {
                await _mongo.Recetas.InsertOneAsync(receta);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarCreateSelects(receta.MedicamentoId);
                return View(receta);
            }
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var receta = await _mongo.Recetas.Find(r => r.Id == oid).FirstOrDefaultAsync();
            if (receta == null) return NotFound();

            await RecargarSelects(receta.ConsultaId, receta.MedicamentoId);
            return View(receta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ConsultaId,MedicamentoId,Cantidad")] Recetas receta)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            ModelState.Remove("ConsultaNavigation");
            ModelState.Remove("MedicamentoNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(receta.ConsultaId, receta.MedicamentoId);
                return View(receta);
            }

            try
            {
                receta.Id = oid;
                await _mongo.Recetas.ReplaceOneAsync(r => r.Id == oid, receta);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(receta.ConsultaId, receta.MedicamentoId);
                return View(receta);
            }
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var receta = await _mongo.Recetas.Find(r => r.Id == oid).FirstOrDefaultAsync();
            if (receta == null) return NotFound();

            receta.ConsultaNavigation    = await _mongo.Consultas.Find(c => c.Id == ObjectId.Parse(receta.ConsultaId)).FirstOrDefaultAsync();
            receta.MedicamentoNavigation = await _mongo.Medicamentos.Find(m => m.Id == ObjectId.Parse(receta.MedicamentoId)).FirstOrDefaultAsync();
            return View(receta);
        }

        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            await _mongo.Recetas.DeleteOneAsync(r => r.Id == oid);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopularNavegacion(List<Recetas> recetas)
        {
            var consultaIds   = recetas.Select(r => r.ConsultaId).Distinct().Select(ObjectId.Parse).ToList();
            var consultas     = await _mongo.Consultas.Find(c => consultaIds.Contains(c.Id)).ToListAsync();
            var pacienteIds   = consultas.Select(c => c.PacienteId).Distinct().Select(ObjectId.Parse).ToList();
            var pacientes     = await _mongo.Pacientes.Find(p => pacienteIds.Contains(p.Id)).ToListAsync();

            var consDict = consultas.ToDictionary(c => c.Id.ToString());
            var pacDict  = pacientes.ToDictionary(p => p.Id.ToString());

            foreach (var c in consultas)
                c.PacienteNavigation = pacDict.GetValueOrDefault(c.PacienteId);

            foreach (var r in recetas)
                r.ConsultaNavigation = consDict.GetValueOrDefault(r.ConsultaId);
        }

        private async Task PopularMedicamentos(List<Recetas> recetas)
        {
            var medIds = recetas.Select(r => r.MedicamentoId).Distinct().Select(ObjectId.Parse).ToList();
            var meds   = await _mongo.Medicamentos.Find(m => medIds.Contains(m.Id)).ToListAsync();
            var medDict = meds.ToDictionary(m => m.Id.ToString());
            foreach (var r in recetas)
                r.MedicamentoNavigation = medDict.GetValueOrDefault(r.MedicamentoId);
        }

        private async Task RecargarCreateSelects(string? selectedMedicamentoId = null)
        {
            var pacientes    = await _mongo.Pacientes.Find(_ => true).SortBy(p => p.Nombre).ToListAsync();
            var medicamentos = await _mongo.Medicamentos.Find(_ => true).SortBy(m => m.Nombre).ToListAsync();

            ViewBag.PacientesList = pacientes;
            ViewBag.Medicamentos  = new SelectList(medicamentos, "IdStr", "Nombre", selectedMedicamentoId);
        }

        private async Task RecargarSelects(string? selectedConsultaId = null, string? selectedMedicamentoId = null)
        {
            var consultas    = await _mongo.Consultas.Find(_ => true).ToListAsync();
            var medicamentos = await _mongo.Medicamentos.Find(_ => true).ToListAsync();

            ViewBag.Consultas = consultas.Select(c => new SelectListItem
            {
                Value    = c.Id.ToString(),
                Text     = c.Diagnostico,
                Selected = c.Id.ToString() == selectedConsultaId
            }).ToList();

            ViewBag.Medicamentos = new SelectList(medicamentos, "IdStr", "Nombre", selectedMedicamentoId);
        }
    }
}
