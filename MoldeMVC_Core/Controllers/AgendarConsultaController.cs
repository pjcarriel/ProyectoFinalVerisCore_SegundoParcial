using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;
using System.Globalization;

namespace MoldeMVC_Core.Controllers
{
    public class AgendarConsultaController : Controller
    {
        private readonly VerisMongoContext _context;
        private const int DURACION_CITA_MINUTOS = 60;

        public AgendarConsultaController(VerisMongoContext context)
        {
            _context = context;
        }

        // GET: AgendarConsulta
        public async Task<IActionResult> Index()
        {
            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .SortBy(p => p.nombre)
                .ToListAsync();

            ViewBag.Especialidades = especialidades;
            ViewBag.Pacientes = pacientes;
            return View();
        }

        // AJAX: médicos por especialidad
        [HttpGet]
        public async Task<IActionResult> GetMedicos(string especialidadId)
        {
            var medicos = await _context.Medicos
                .Find(m => m.especialidadId == especialidadId)
                .SortBy(m => m.nombre)
                .ToListAsync();

            return Json(medicos.Select(m => new { id = m._id, nombre = m.nombre }));
        }

        // AJAX: disponibilidad semanal del médico
        [HttpGet]
        public async Task<IActionResult> GetCalendario(string medicoId, int anio, int semana)
        {
            if (semana < 1 || semana > 53 || anio < 2000 || anio > 2030)
                return Json(new { error = "Parámetros inválidos." });

            var medico = await _context.Medicos.Find(m => m._id == medicoId).FirstOrDefaultAsync();
            if (medico == null) return Json(new { error = "Médico no encontrado." });

            var especialidad = await _context.Especialidades.Find(e => e._id == medico.especialidadId).FirstOrDefaultAsync();
            if (especialidad == null) return Json(new { error = "Especialidad no encontrada." });

            var lunes = ISOWeek.ToDateTime(anio, semana, DayOfWeek.Monday);
            var sabado = lunes.AddDays(5);

            if (!TimeSpan.TryParse(especialidad.franjaHI, out var hi) ||
                !TimeSpan.TryParse(especialidad.franjaHF, out var hf))
                return Json(new { error = "Horario de especialidad inválido." });

            int totalSlotsDia = (int)((hf - hi).TotalMinutes / DURACION_CITA_MINUTOS);

            var consultasSemana = await _context.Consultas
                .Find(Builders<Consultas>.Filter.And(
                    Builders<Consultas>.Filter.Eq(c => c.medicoId, medicoId),
                    Builders<Consultas>.Filter.Gte(c => c.fechaConsulta, lunes),
                    Builders<Consultas>.Filter.Lt(c => c.fechaConsulta, sabado)))
                .ToListAsync();

            var diasTrabajo = ParseDias(especialidad.dias);

            var diasInfo = new List<object>();
            for (int i = 0; i < 5; i++)
            {
                var dia = lunes.AddDays(i);
                bool esDiaTrabajo = diasTrabajo.Contains(dia.DayOfWeek);
                bool esPasado = dia.Date < DateTime.Today;

                int ocupados = consultasSemana.Count(c => c.fechaConsulta.Date == dia.Date);
                int libres = esDiaTrabajo ? Math.Max(0, totalSlotsDia - ocupados) : 0;

                diasInfo.Add(new
                {
                    fecha = dia.ToString("yyyy-MM-dd"),
                    disponible = esDiaTrabajo && !esPasado && libres > 0,
                    slotsLibres = (esDiaTrabajo && !esPasado) ? libres : 0,
                    totalSlots = totalSlotsDia,
                    esPasado
                });
            }

            return Json(new
            {
                dias = diasInfo,
                lunesSemana = lunes.ToString("dd/MM/yyyy"),
                viernesSemana = lunes.AddDays(4).ToString("dd/MM/yyyy"),
                semana,
                anio
            });
        }

        // AJAX: horas disponibles en un día
        [HttpGet]
        public async Task<IActionResult> GetHoras(string medicoId, string fecha)
        {
            if (!DateTime.TryParse(fecha, out var fechaDt))
                return Json(new List<string>());

            // No permitir fines de semana
            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new List<string>());

            var medico = await _context.Medicos.Find(m => m._id == medicoId).FirstOrDefaultAsync();
            if (medico == null) return Json(new List<string>());

            var especialidad = await _context.Especialidades.Find(e => e._id == medico.especialidadId).FirstOrDefaultAsync();
            if (especialidad == null) return Json(new List<string>());

            if (!TimeSpan.TryParse(especialidad.franjaHI, out var hi) ||
                !TimeSpan.TryParse(especialidad.franjaHF, out var hf))
                return Json(new List<string>());

            // Generar todos los slots
            var todosSlots = new List<string>();
            var current = hi;
            while (current.Add(TimeSpan.FromMinutes(DURACION_CITA_MINUTOS)) <= hf)
            {
                todosSlots.Add(current.ToString(@"hh\:mm"));
                current = current.Add(TimeSpan.FromMinutes(DURACION_CITA_MINUTOS));
            }

            // Obtener slots ya reservados
            var startOfDay = fechaDt.Date;
            var endOfDay = fechaDt.Date.AddDays(1);
            var reservados = await _context.Consultas
                .Find(Builders<Consultas>.Filter.And(
                    Builders<Consultas>.Filter.Eq(c => c.medicoId, medicoId),
                    Builders<Consultas>.Filter.Gte(c => c.fechaConsulta, startOfDay),
                    Builders<Consultas>.Filter.Lt(c => c.fechaConsulta, endOfDay)))
                .Project(c => c.hi)
                .ToListAsync();

            return Json(todosSlots.Except(reservados).ToList());
        }

        // POST: confirmar cita
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

            if (!DateTime.TryParse(fecha, out var fechaDt))
                return Json(new { ok = false, mensaje = "Fecha inválida." });

            // Regla: no fechas pasadas
            if (fechaDt.Date < DateTime.Today)
                return Json(new { ok = false, mensaje = "No puedes agendar una cita en una fecha pasada." });

            // Regla: sin fines de semana
            if (fechaDt.DayOfWeek == DayOfWeek.Saturday || fechaDt.DayOfWeek == DayOfWeek.Sunday)
                return Json(new { ok = false, mensaje = "No hay atención los sábados ni domingos." });

            var startOfDay = fechaDt.Date;
            var endOfDay = fechaDt.Date.AddDays(1);

            // Regla: paciente sin cita doble ese día y hora
            var conflictoPaciente = await _context.Consultas
                .Find(Builders<Consultas>.Filter.And(
                    Builders<Consultas>.Filter.Eq(c => c.pacienteId, pacienteId),
                    Builders<Consultas>.Filter.Gte(c => c.fechaConsulta, startOfDay),
                    Builders<Consultas>.Filter.Lt(c => c.fechaConsulta, endOfDay),
                    Builders<Consultas>.Filter.Eq(c => c.hi, hora)))
                .AnyAsync();

            if (conflictoPaciente)
                return Json(new { ok = false, mensaje = "El paciente ya tiene una cita agendada a esa hora ese día." });

            // Regla: médico sin cita doble ese día y hora
            var conflictoMedico = await _context.Consultas
                .Find(Builders<Consultas>.Filter.And(
                    Builders<Consultas>.Filter.Eq(c => c.medicoId, medicoId),
                    Builders<Consultas>.Filter.Gte(c => c.fechaConsulta, startOfDay),
                    Builders<Consultas>.Filter.Lt(c => c.fechaConsulta, endOfDay),
                    Builders<Consultas>.Filter.Eq(c => c.hi, hora)))
                .AnyAsync();

            if (conflictoMedico)
                return Json(new { ok = false, mensaje = "Ese horario ya fue reservado para este médico. Elige otra hora." });

            // Calcular hora fin (cita de 1 hora)
            var hiTs = TimeSpan.Parse(hora);
            var hfStr = hiTs.Add(TimeSpan.FromMinutes(DURACION_CITA_MINUTOS)).ToString(@"hh\:mm");

            var consulta = new Consultas
            {
                _id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                medicoId = medicoId,
                pacienteId = pacienteId,
                fechaConsulta = fechaDt,
                hi = hora,
                hf = hfStr,
                diagnostico = "Pendiente"
            };

            await _context.Consultas.InsertOneAsync(consulta);

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
                    case 'L': result.Add(DayOfWeek.Monday); break;
                    case 'M': result.Add(DayOfWeek.Tuesday); break;
                    case 'X': result.Add(DayOfWeek.Wednesday); break;
                    case 'J': result.Add(DayOfWeek.Thursday); break;
                    case 'V': result.Add(DayOfWeek.Friday); break;
                }
            }
            return result;
        }
    }
}
