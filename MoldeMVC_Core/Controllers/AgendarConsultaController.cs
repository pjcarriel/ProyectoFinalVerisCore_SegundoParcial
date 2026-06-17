using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;
using System.Globalization;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Medico,SuperAdmin,Paciente")]
    public class AgendarConsultaController : Controller
    {
        private readonly MongoDbContext _mongo;
        private const int DURACION_CITA_MINUTOS = 60;

        public AgendarConsultaController(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> Index()
        {
            var especialidades = await _mongo.Especialidades.Find(_ => true).ToListAsync();

            List<Pacientes> pacientes;
            if (User.IsInRole("Paciente"))
            {
                var sesion  = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (!int.TryParse(objUser?.PhoneNumber, out var cedula))
                    pacientes = new List<Pacientes>();
                else
                    pacientes = await _mongo.Pacientes
                        .Find(p => p.Cedula == cedula)
                        .SortBy(p => p.Nombre)
                        .ToListAsync();
            }
            else
            {
                pacientes = await _mongo.Pacientes.Find(_ => true).SortBy(p => p.Nombre).ToListAsync();
            }

            ViewBag.Especialidades = especialidades;
            ViewBag.Pacientes      = pacientes;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicos(string especialidadId)
        {
            var medicos = await _mongo.Medicos
                .Find(m => m.EspecialidadId == especialidadId)
                .SortBy(m => m.Nombre)
                .ToListAsync();

            return Json(medicos.Select(m => new { id = m.IdStr, nombre = m.Nombre }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendario(string medicoId, int anio, int semana)
        {
            if (semana < 1 || semana > 53 || anio < 2000 || anio > 2030)
                return Json(new { error = "Parámetros inválidos." });

            if (!ObjectId.TryParse(medicoId, out var medicoOid))
                return Json(new { error = "Médico no encontrado." });

            var medico = await _mongo.Medicos.Find(m => m.Id == medicoOid).FirstOrDefaultAsync();
            if (medico == null) return Json(new { error = "Médico no encontrado." });

            medico.EspecialidadNavigation = await _mongo.Especialidades
                .Find(e => e.Id == ObjectId.Parse(medico.EspecialidadId))
                .FirstOrDefaultAsync();

            var especialidad = medico.EspecialidadNavigation;
            if (especialidad == null) return Json(new { error = "Especialidad no encontrada." });

            var lunes  = DateOnly.FromDateTime(ISOWeek.ToDateTime(anio, semana, DayOfWeek.Monday));
            var sabado = lunes.AddDays(5);

            var hi = especialidad.FranjaHi;
            var hf = especialidad.FranjaHf;
            int totalSlotsDia = (int)((hf.ToTimeSpan() - hi.ToTimeSpan()).TotalMinutes / DURACION_CITA_MINUTOS);

            var consultasSemana = await _mongo.Consultas
                .Find(c => c.MedicoId == medicoId && c.FechaConsulta >= lunes && c.FechaConsulta < sabado)
                .ToListAsync();

            var diasTrabajo = ParseDias(especialidad.Dias);

            var diasInfo = new List<object>();
            for (int i = 0; i < 5; i++)
            {
                var dia          = lunes.AddDays(i);
                bool esDiaTrabajo = diasTrabajo.Contains(dia.DayOfWeek);
                bool esPasado    = dia < DateOnly.FromDateTime(DateTime.Today);

                int ocupados = consultasSemana.Count(c => c.FechaConsulta == dia);
                int libres   = esDiaTrabajo ? Math.Max(0, totalSlotsDia - ocupados) : 0;

                diasInfo.Add(new
                {
                    fecha       = dia.ToString("yyyy-MM-dd"),
                    disponible  = esDiaTrabajo && !esPasado && libres > 0,
                    slotsLibres = (esDiaTrabajo && !esPasado) ? libres : 0,
                    totalSlots  = totalSlotsDia,
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

        [HttpGet]
        public async Task<IActionResult> GetHoras(string medicoId, string fecha)
        {
            if (!DateOnly.TryParse(fecha, out var fechaDt))
                return Json(new List<string>());

            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new List<string>());

            if (!ObjectId.TryParse(medicoId, out var medicoOid))
                return Json(new List<string>());

            var medico = await _mongo.Medicos.Find(m => m.Id == medicoOid).FirstOrDefaultAsync();
            if (medico == null) return Json(new List<string>());

            medico.EspecialidadNavigation = await _mongo.Especialidades
                .Find(e => e.Id == ObjectId.Parse(medico.EspecialidadId))
                .FirstOrDefaultAsync();

            if (medico.EspecialidadNavigation == null) return Json(new List<string>());

            var especialidad = medico.EspecialidadNavigation;
            var hi = especialidad.FranjaHi;
            var hf = especialidad.FranjaHf;

            var todosSlots = new List<TimeOnly>();
            var current = hi;
            while (current.AddMinutes(DURACION_CITA_MINUTOS) <= hf)
            {
                todosSlots.Add(current);
                current = current.AddMinutes(DURACION_CITA_MINUTOS);
            }

            var consultas  = await _mongo.Consultas
                .Find(c => c.MedicoId == medicoId && c.FechaConsulta == fechaDt)
                .ToListAsync();
            var reservados = consultas.Select(c => c.Hi).ToList();

            return Json(todosSlots
                .Except(reservados)
                .Select(t => t.ToString(@"HH\:mm"))
                .ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(
            [FromForm] string medicoId,
            [FromForm] string pacienteId,
            [FromForm] string fecha,
            [FromForm] string hora)
        {
            if (string.IsNullOrWhiteSpace(medicoId) || string.IsNullOrWhiteSpace(pacienteId) ||
                string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(hora))
                return Json(new { ok = false, mensaje = "Todos los campos son obligatorios." });

            if (!ObjectId.TryParse(medicoId, out _) || !ObjectId.TryParse(pacienteId, out _))
                return Json(new { ok = false, mensaje = "Todos los campos son obligatorios." });

            if (!DateOnly.TryParse(fecha, out var fechaDt))
                return Json(new { ok = false, mensaje = "Fecha inválida." });

            if (fechaDt < DateOnly.FromDateTime(DateTime.Today))
                return Json(new { ok = false, mensaje = "No puedes agendar una cita en una fecha pasada." });

            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new { ok = false, mensaje = "No hay atención los sábados ni domingos." });

            if (!TimeOnly.TryParse(hora, out var hiTime))
                return Json(new { ok = false, mensaje = "Hora inválida." });

            var conflictoPaciente = await _mongo.Consultas
                .CountDocumentsAsync(c => c.PacienteId == pacienteId && c.FechaConsulta == fechaDt && c.Hi == hiTime) > 0;

            if (conflictoPaciente)
                return Json(new { ok = false, mensaje = "El paciente ya tiene una cita agendada a esa hora ese día." });

            var conflictoMedico = await _mongo.Consultas
                .CountDocumentsAsync(c => c.MedicoId == medicoId && c.FechaConsulta == fechaDt && c.Hi == hiTime) > 0;

            if (conflictoMedico)
                return Json(new { ok = false, mensaje = "Ese horario ya fue reservado para este médico. Elige otra hora." });

            var consulta = new Consultas
            {
                MedicoId      = medicoId,
                PacienteId    = pacienteId,
                FechaConsulta = fechaDt,
                Hi            = hiTime,
                Hf            = hiTime.AddMinutes(DURACION_CITA_MINUTOS),
                Diagnostico   = "Pendiente"
            };

            await _mongo.Consultas.InsertOneAsync(consulta);
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
