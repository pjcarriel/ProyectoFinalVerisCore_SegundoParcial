using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Medico,SuperAdmin,Paciente")]
    public class RecetasController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;

        public RecetasController(ProyectoVerisMvcBdContext context)
        {
            _context = context;
        }

        // GET: Recetas
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.IdUsuario == userId);
                if (paciente == null) return View(new List<Receta>());

                var misRecetas = await _context.Recetas
                    .Include(r => r.IdConsultaNavigation)
                        .ThenInclude(c => c!.IdPacienteNavigation)
                    .Include(r => r.IdMedicamentoNavigation)
                    .Where(r => r.IdConsultaNavigation!.IdPaciente == paciente.IdPaciente)
                    .ToListAsync();
                return View(misRecetas);
            }

            var recetas = await _context.Recetas
                .Include(r => r.IdConsultaNavigation)
                .Include(r => r.IdMedicamentoNavigation)
                .ToListAsync();

            return View(recetas);
        }

        // GET: Recetas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var receta = await _context.Recetas
                .Include(r => r.IdConsultaNavigation)
                    .ThenInclude(c => c!.IdPacienteNavigation)
                .Include(r => r.IdMedicamentoNavigation)
                .FirstOrDefaultAsync(r => r.IdReceta == id);

            if (receta == null)
                return NotFound();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                if (receta.IdConsultaNavigation?.IdPacienteNavigation?.IdUsuario != userId)
                    return Forbid();
            }

            ViewBag.ConsultaNombre    = receta.IdConsultaNavigation?.Diagnostico ?? "";
            ViewBag.MedicamentoNombre = receta.IdMedicamentoNavigation?.Nombre ?? "";

            return View(receta);
        }

        // GET: Recetas/Create
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await RecargarCreateSelects();
            return View();
        }

        // AJAX: consultas de un paciente
        [HttpGet]
        public async Task<IActionResult> GetConsultasPaciente(int pacienteId)
        {
            var consultas = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .Where(c => c.IdPaciente == pacienteId)
                .OrderByDescending(c => c.FechaConsulta)
                .ToListAsync();

            var resultado = consultas.Select(c => new
            {
                id          = c.IdConsulta,
                texto       = $"{c.FechaConsulta:dd/MM/yyyy}  {c.Hi:hh\\:mm}–{c.Hf:hh\\:mm}  |  {c.IdMedicoNavigation?.Nombre ?? "?"}",
                diagnostico = c.Diagnostico ?? "Pendiente",
                pendiente   = string.IsNullOrWhiteSpace(c.Diagnostico) || c.Diagnostico.Trim().ToLower() == "pendiente"
            });

            return Json(resultado);
        }

        // AJAX: diagnóstico de una consulta
        [HttpGet]
        public async Task<IActionResult> GetDiagnostico(int consultaId)
        {
            var consulta = await _context.Consultas
                .FirstOrDefaultAsync(c => c.IdConsulta == consultaId);

            if (consulta == null)
                return Json(new { diagnostico = "", pendiente = true });

            bool pendiente = string.IsNullOrWhiteSpace(consulta.Diagnostico) ||
                             consulta.Diagnostico.Trim().ToLower() == "pendiente";

            return Json(new { diagnostico = consulta.Diagnostico ?? "Pendiente", pendiente });
        }

        // POST: Recetas/Create
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdConsulta,IdMedicamento,Cantidad")] Receta receta)
        {
            ModelState.Remove("IdConsultaNavigation");
            ModelState.Remove("IdMedicamentoNavigation");

            if (receta.IdConsulta > 0)
            {
                var consulta = await _context.Consultas
                    .FirstOrDefaultAsync(c => c.IdConsulta == receta.IdConsulta);

                if (consulta == null ||
                    string.IsNullOrWhiteSpace(consulta.Diagnostico) ||
                    consulta.Diagnostico.Trim().ToLower() == "pendiente")
                {
                    ModelState.AddModelError("", "La consulta seleccionada aún no tiene diagnóstico registrado. Registre el diagnóstico antes de crear la receta.");
                }
            }

            if (!ModelState.IsValid)
            {
                await RecargarCreateSelects(receta.IdMedicamento);
                return View(receta);
            }

            try
            {
                _context.Add(receta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarCreateSelects(receta.IdMedicamento);
                return View(receta);
            }
        }

        // GET: Recetas/Edit/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var receta = await _context.Recetas
                .FirstOrDefaultAsync(r => r.IdReceta == id);

            if (receta == null)
                return NotFound();

            await RecargarSelects(receta.IdConsulta, receta.IdMedicamento);
            return View(receta);
        }

        // POST: Recetas/Edit/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdReceta,IdConsulta,IdMedicamento,Cantidad")] Receta receta)
        {
            if (id != receta.IdReceta)
                return NotFound();

            ModelState.Remove("IdConsultaNavigation");
            ModelState.Remove("IdMedicamentoNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(receta.IdConsulta, receta.IdMedicamento);
                return View(receta);
            }

            try
            {
                _context.Update(receta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Recetas.AnyAsync(r => r.IdReceta == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(receta.IdConsulta, receta.IdMedicamento);
                return View(receta);
            }
        }

        // GET: Recetas/Delete/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var receta = await _context.Recetas
                .Include(r => r.IdConsultaNavigation)
                .Include(r => r.IdMedicamentoNavigation)
                .FirstOrDefaultAsync(r => r.IdReceta == id);

            if (receta == null)
                return NotFound();

            return View(receta);
        }

        // POST: Recetas/Delete/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var receta = await _context.Recetas
                .FirstOrDefaultAsync(r => r.IdReceta == id);

            if (receta == null)
                return NotFound();

            _context.Remove(receta);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarCreateSelects(int selectedMedicamentoId = 0)
        {
            var pacientes    = await _context.Pacientes.OrderBy(p => p.Nombre).ToListAsync();
            var medicamentos = await _context.Medicamentos.OrderBy(m => m.Nombre).ToListAsync();

            ViewBag.PacientesList = pacientes;
            ViewBag.Medicamentos  = new SelectList(medicamentos, "IdMedicamento", "Nombre", selectedMedicamentoId);
        }

        private async Task RecargarSelects(int selectedConsultaId = 0, int selectedMedicamentoId = 0)
        {
            var consultas    = await _context.Consultas.ToListAsync();
            var medicamentos = await _context.Medicamentos.ToListAsync();

            ViewBag.Consultas = consultas.Select(c => new SelectListItem
            {
                Value    = c.IdConsulta.ToString(),
                Text     = c.Diagnostico,
                Selected = c.IdConsulta == selectedConsultaId
            }).ToList();

            ViewBag.Medicamentos = new SelectList(medicamentos, "IdMedicamento", "Nombre", selectedMedicamentoId);
        }
    }
}
