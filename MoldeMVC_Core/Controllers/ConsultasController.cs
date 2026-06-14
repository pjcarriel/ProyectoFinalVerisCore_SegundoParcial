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
    public class ConsultasController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;

        public ConsultasController(ProyectoVerisMvcBdContext context)
        {
            _context = context;
        }

        // GET: Consultas
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.IdUsuario == userId);
                if (paciente == null) return View(new List<Consulta>());

                var misConsultas = await _context.Consultas
                    .Include(c => c.IdMedicoNavigation)
                    .Include(c => c.IdPacienteNavigation)
                    .Where(c => c.IdPaciente == paciente.IdPaciente)
                    .ToListAsync();
                return View(misConsultas);
            }

            var consultas = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .Include(c => c.IdPacienteNavigation)
                .ToListAsync();

            return View(consultas);
        }

        // GET: Consultas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var consulta = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .Include(c => c.IdPacienteNavigation)
                .FirstOrDefaultAsync(c => c.IdConsulta == id);

            if (consulta == null)
                return NotFound();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                if (consulta.IdPacienteNavigation?.IdUsuario != userId)
                    return Forbid();
            }

            ViewBag.MedicoNombre   = consulta.IdMedicoNavigation?.Nombre ?? "";
            ViewBag.PacienteNombre = consulta.IdPacienteNavigation?.Nombre ?? "";

            return View(consulta);
        }

        // GET: Consultas/Create
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await RecargarSelects();
            return View();
        }

        // POST: Consultas/Create
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdMedico,IdPaciente,FechaConsulta,Hi,Hf,Diagnostico")] Consulta consulta)
        {
            ModelState.Remove("IdMedicoNavigation");
            ModelState.Remove("IdPacienteNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consulta.IdMedico, consulta.IdPaciente);
                return View(consulta);
            }

            try
            {
                _context.Add(consulta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consulta.IdMedico, consulta.IdPaciente);
                return View(consulta);
            }
        }

        // GET: Consultas/Edit/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var consulta = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .FirstOrDefaultAsync(c => c.IdConsulta == id);

            if (consulta == null)
                return NotFound();

            var especialidades = await _context.Especialidades.ToListAsync();
            var pacientes = await _context.Pacientes.OrderBy(p => p.Nombre).ToListAsync();

            ViewBag.Especialidades    = especialidades;
            ViewBag.PacientesList     = pacientes;
            ViewBag.MedicoEspecialidadId = consulta.IdMedicoNavigation?.IdEspecialidad ?? 0;
            ViewBag.MedicoNombre      = consulta.IdMedicoNavigation?.Nombre ?? "";

            return View(consulta);
        }

        // POST: Consultas/Edit/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdConsulta,IdMedico,IdPaciente,FechaConsulta,Hi,Hf,Diagnostico")] Consulta consulta)
        {
            if (id != consulta.IdConsulta)
                return NotFound();

            ModelState.Remove("IdMedicoNavigation");
            ModelState.Remove("IdPacienteNavigation");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consulta.IdMedico, consulta.IdPaciente);
                return View(consulta);
            }

            try
            {
                _context.Update(consulta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Consultas.AnyAsync(c => c.IdConsulta == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consulta.IdMedico, consulta.IdPaciente);
                return View(consulta);
            }
        }

        // GET: Consultas/Atender/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        public async Task<IActionResult> Atender(int id)
        {
            var consulta = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .Include(c => c.IdPacienteNavigation)
                .FirstOrDefaultAsync(c => c.IdConsulta == id);

            if (consulta == null)
                return NotFound();

            ViewBag.MedicoNombre   = consulta.IdMedicoNavigation?.Nombre ?? "—";
            ViewBag.PacienteNombre = consulta.IdPacienteNavigation?.Nombre ?? "—";
            ViewBag.MedicoFoto     = consulta.IdMedicoNavigation?.Foto ?? "";
            ViewBag.PacienteFoto   = consulta.IdPacienteNavigation?.Foto ?? "";

            return View(consulta);
        }

        // POST: Consultas/Atender/5
        [Authorize(Roles = "Medico,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atender(int id, string diagnostico)
        {
            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                ModelState.AddModelError("diagnostico", "El diagnóstico no puede estar vacío.");
                var c = await _context.Consultas
                    .Include(c => c.IdMedicoNavigation)
                    .Include(c => c.IdPacienteNavigation)
                    .FirstOrDefaultAsync(c => c.IdConsulta == id);
                ViewBag.MedicoNombre   = c?.IdMedicoNavigation?.Nombre ?? "—";
                ViewBag.PacienteNombre = c?.IdPacienteNavigation?.Nombre ?? "—";
                ViewBag.MedicoFoto     = c?.IdMedicoNavigation?.Foto ?? "";
                ViewBag.PacienteFoto   = c?.IdPacienteNavigation?.Foto ?? "";
                return View(c);
            }

            var consulta = await _context.Consultas.FirstOrDefaultAsync(c => c.IdConsulta == id);
            if (consulta == null)
                return NotFound();

            consulta.Diagnostico = diagnostico.Trim();
            _context.Update(consulta);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Consultas/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var consulta = await _context.Consultas
                .Include(c => c.IdMedicoNavigation)
                .Include(c => c.IdPacienteNavigation)
                .FirstOrDefaultAsync(c => c.IdConsulta == id);

            if (consulta == null)
                return NotFound();

            return View(consulta);
        }

        // POST: Consultas/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consulta = await _context.Consultas
                .FirstOrDefaultAsync(c => c.IdConsulta == id);

            if (consulta == null)
                return NotFound();

            _context.Remove(consulta);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarSelects(int selectedMedicoId = 0, int selectedPacienteId = 0)
        {
            var medicos   = await _context.Medicos.ToListAsync();
            var pacientes = await _context.Pacientes.ToListAsync();

            ViewBag.Medicos   = new SelectList(medicos,   "IdMedico",   "Nombre", selectedMedicoId);
            ViewBag.Pacientes = new SelectList(pacientes, "IdPaciente", "Nombre", selectedPacienteId);
        }
    }
}
