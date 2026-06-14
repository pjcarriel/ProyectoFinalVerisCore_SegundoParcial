using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;
using System.Globalization;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Medico,SuperAdmin,Paciente")]
    public class AgendarConsultaController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;
        private const int DURACION_CITA_MINUTOS = 60;

        public AgendarConsultaController(ProyectoVerisMvcBdContext context)
        {
            _context = context;
        }

        // GET: AgendarConsulta
        public async Task<IActionResult> Index()
        {
            var especialidades = await _context.Especialidades.ToListAsync();

            List<Paciente> pacientes;
            if (User.IsInRole("Paciente"))
            {
                var sesion  = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId  = objUser?.Id;
                pacientes = await _context.Pacientes
                    .Where(p => p.IdUsuario == userId)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            else
            {
                pacientes = await _context.Pacientes.OrderBy(p => p.Nombre).ToListAsync();
            }

            ViewBag.Especialidades = especialidades;
            ViewBag.Pacientes      = pacientes;
            return View();
        }

        // AJAX: médicos por especialidad
        [HttpGet]
        public async Task<IActionResult> GetMedicos(int especialidadId)
        {
            var medicos = await _context.Medicos
                .Where(m => m.IdEspecialidad == especialidadId)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return Json(medicos.Select(m => new { id = m.IdMedico, nombre = m.Nombre }));
        }

        // AJAX: disponibilidad semanal del médico
        [HttpGet]
        public async Task<IActionResult> GetCalendario(int medicoId, int anio, int semana)
        {
            if (semana < 1 || semana > 53 || anio < 2000 || anio > 2030)
                return Json(new { error = "Parámetros inválidos." });

            var medico = await _context.Medicos
                .Include(m => m.IdEspecialidadNavigation)
                .FirstOrDefaultAsync(m => m.IdMedico == medicoId);

            if (medico == null)
                return Json(new { error = "Médico no encontrado." });

            var especialidad = medico.IdEspecialidadNavigation;
            if (especialidad == null)
                return Json(new { error = "Especialidad no encontrada." });

            var lunes  = DateOnly.FromDateTime(ISOWeek.ToDateTime(anio, semana, DayOfWeek.Monday));
            var sabado = lunes.AddDays(5);

            var hi = especialidad.FranjaHi;
            var hf = especialidad.FranjaHf;
            int totalSlotsDia = (int)((hf.ToTimeSpan() - hi.ToTimeSpan()).TotalMinutes / DURACION_CITA_MINUTOS);

            var consultasSemana = await _context.Consultas
                .Where(c => c.IdMedico == medicoId && c.FechaConsulta >= lunes && c.FechaConsulta < sabado)
                .ToListAsync();

            var diasTrabajo = ParseDias(especialidad.Dias);

            var diasInfo = new List<object>();
            for (int i = 0; i < 5; i++)
            {
                var dia         = lunes.AddDays(i);
                bool esDiaTrabajo = diasTrabajo.Contains(dia.DayOfWeek);
                bool esPasado   = dia < DateOnly.FromDateTime(DateTime.Today);

                int ocupados = consultasSemana.Count(c => c.FechaConsulta == dia);
                int libres   = esDiaTrabajo ? Math.Max(0, totalSlotsDia - ocupados) : 0;

                diasInfo.Add(new
                {
                    fecha        = dia.ToString("yyyy-MM-dd"),
                    disponible   = esDiaTrabajo && !esPasado && libres > 0,
                    slotsLibres  = (esDiaTrabajo && !esPasado) ? libres : 0,
                    totalSlots   = totalSlotsDia,
                    esPasado
                });
            }

            return Json(new
            {
                dias          = diasInfo,
                lunesSemana   = lunes.ToString("dd/MM/yyyy"),
                viernesSemana = lunes.AddDays(4).ToString("dd/MM/yyyy"),
                semana,
                anio
            });
        }

        // AJAX: horas disponibles en un día
        [HttpGet]
        public async Task<IActionResult> GetHoras(int medicoId, string fecha)
        {
            if (!DateOnly.TryParse(fecha, out var fechaDt))
                return Json(new List<string>());

            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new List<string>());

            var medico = await _context.Medicos
                .Include(m => m.IdEspecialidadNavigation)
                .FirstOrDefaultAsync(m => m.IdMedico == medicoId);

            if (medico?.IdEspecialidadNavigation == null)
                return Json(new List<string>());

            var especialidad = medico.IdEspecialidadNavigation;
            var hi = especialidad.FranjaHi;
            var hf = especialidad.FranjaHf;

            var todosSlots = new List<TimeOnly>();
            var current = hi;
            while (current.AddMinutes(DURACION_CITA_MINUTOS) <= hf)
            {
                todosSlots.Add(current);
                current = current.AddMinutes(DURACION_CITA_MINUTOS);
            }

            var reservados = await _context.Consultas
                .Where(c => c.IdMedico == medicoId && c.FechaConsulta == fechaDt)
                .Select(c => c.Hi)
                .ToListAsync();

            return Json(todosSlots
                .Except(reservados)
                .Select(t => t.ToString(@"hh\:mm"))
                .ToList());
        }

        // POST: confirmar cita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(
            [FromForm] int medicoId,
            [FromForm] int pacienteId,
            [FromForm] string fecha,
            [FromForm] string hora)
        {
            if (medicoId == 0 || pacienteId == 0 ||
                string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(hora))
                return Json(new { ok = false, mensaje = "Todos los campos son obligatorios." });

            if (!DateOnly.TryParse(fecha, out var fechaDt))
                return Json(new { ok = false, mensaje = "Fecha inválida." });

            if (fechaDt < DateOnly.FromDateTime(DateTime.Today))
                return Json(new { ok = false, mensaje = "No puedes agendar una cita en una fecha pasada." });

            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new { ok = false, mensaje = "No hay atención los sábados ni domingos." });

            if (!TimeOnly.TryParse(hora, out var hiTime))
                return Json(new { ok = false, mensaje = "Hora inválida." });

            var conflictoPaciente = await _context.Consultas
                .AnyAsync(c => c.IdPaciente == pacienteId && c.FechaConsulta == fechaDt && c.Hi == hiTime);

            if (conflictoPaciente)
                return Json(new { ok = false, mensaje = "El paciente ya tiene una cita agendada a esa hora ese día." });

            var conflictoMedico = await _context.Consultas
                .AnyAsync(c => c.IdMedico == medicoId && c.FechaConsulta == fechaDt && c.Hi == hiTime);

            if (conflictoMedico)
                return Json(new { ok = false, mensaje = "Ese horario ya fue reservado para este médico. Elige otra hora." });

            var consulta = new Consulta
            {
                IdMedico      = medicoId,
                IdPaciente    = pacienteId,
                FechaConsulta = fechaDt,
                Hi            = hiTime,
                Hf            = hiTime.AddMinutes(DURACION_CITA_MINUTOS),
                Diagnostico   = "Pendiente"
            };

            _context.Add(consulta);
            await _context.SaveChangesAsync();

            return Json(new { ok = true, mensaje = "¡Cita agendada exitosamente!" });
        }

        private static List<DayOfWeek> ParseDias(string dias)
        {
            var result = new List<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(dias)) return result;
            foreach (char c in dias.ToUpper())
            {
                switch (c)
                {
                    case 'L': result.Add(DayOfWeek.Monday);    break;
                    case 'M': result.Add(DayOfWeek.Tuesday);   break;
                    case 'X': result.Add(DayOfWeek.Wednesday); break;
                    case 'J': result.Add(DayOfWeek.Thursday);  break;
                    case 'V': result.Add(DayOfWeek.Friday);    break;
                }
            }
            return result;
        }
    }
}
