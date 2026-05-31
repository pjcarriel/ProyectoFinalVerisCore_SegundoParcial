using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class ConsultasController : Controller
    {
        private readonly VerisMongoContext _context;

        public ConsultasController(VerisMongoContext context)
        {
            _context = context;
        }

        // GET: Consultas
        public async Task<IActionResult> Index()
        {
            var consultas = await _context.Consultas
                .Find(Builders<Consultas>.Filter.Empty)
                .ToListAsync();

            var medicos = await _context.Medicos
                .Find(Builders<Medicos>.Filter.Empty)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .ToListAsync();

            ViewBag.MedicosDict = medicos.ToDictionary(m => m._id, m => m.nombre);
            ViewBag.PacientesDict = pacientes.ToDictionary(p => p._id, p => p.nombre);

            return View(consultas);
        }

        // GET: Consultas/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var consultas = await _context.Consultas
                .Find(c => c._id == id)
                .FirstOrDefaultAsync();

            if (consultas == null)
            {
                return NotFound();
            }

            var medico = await _context.Medicos
                .Find(m => m._id == consultas.medicoId)
                .FirstOrDefaultAsync();

            var paciente = await _context.Pacientes
                .Find(p => p._id == consultas.pacienteId)
                .FirstOrDefaultAsync();

            ViewBag.MedicoNombre = medico?.nombre ?? consultas.medicoId;
            ViewBag.PacienteNombre = paciente?.nombre ?? consultas.pacienteId;

            return View(consultas);
        }

        // GET: Consultas/Create
        public async Task<IActionResult> Create()
        {
            await RecargarSelects();
            return View();
        }

        // POST: Consultas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("medicoId,pacienteId,fechaConsulta,hi,hf,diagnostico")] Consultas consultas)
        {
            consultas._id = ObjectId.GenerateNewId().ToString();

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            if (!ObjectId.TryParse(consultas.medicoId, out _))
            {
                ModelState.AddModelError("medicoId", "El médico seleccionado no es válido.");
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            if (!ObjectId.TryParse(consultas.pacienteId, out _))
            {
                ModelState.AddModelError("pacienteId", "El paciente seleccionado no es válido.");
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            try
            {
                await _context.Consultas.InsertOneAsync(consultas);

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al guardar en MongoDB: " + ex.Message);
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }
        }

        // GET: Consultas/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var consultas = await _context.Consultas
                .Find(c => c._id == id)
                .FirstOrDefaultAsync();

            if (consultas == null)
            {
                return NotFound();
            }

            var medico = await _context.Medicos
                .Find(m => m._id == consultas.medicoId)
                .FirstOrDefaultAsync();

            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .SortBy(p => p.nombre)
                .ToListAsync();

            ViewBag.Especialidades = especialidades;
            ViewBag.PacientesList = pacientes;
            ViewBag.MedicoEspecialidadId = medico?.especialidadId ?? "";
            ViewBag.MedicoNombre = medico?.nombre ?? "";

            return View(consultas);
        }

        // POST: Consultas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,medicoId,pacienteId,fechaConsulta,hi,hf,diagnostico")] Consultas consultas)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != consultas._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            if (!ObjectId.TryParse(consultas.medicoId, out _))
            {
                ModelState.AddModelError("medicoId", "El médico seleccionado no es válido.");
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            if (!ObjectId.TryParse(consultas.pacienteId, out _))
            {
                ModelState.AddModelError("pacienteId", "El paciente seleccionado no es válido.");
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }

            try
            {
                var resultado = await _context.Consultas
                    .ReplaceOneAsync(c => c._id == id, consultas);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(consultas.medicoId, consultas.pacienteId);
                return View(consultas);
            }
        }

        // GET: Consultas/Atender/5
        public async Task<IActionResult> Atender(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
                return NotFound();

            var consulta = await _context.Consultas
                .Find(c => c._id == id)
                .FirstOrDefaultAsync();

            if (consulta == null)
                return NotFound();

            var medico = await _context.Medicos
                .Find(m => m._id == consulta.medicoId)
                .FirstOrDefaultAsync();

            var paciente = await _context.Pacientes
                .Find(p => p._id == consulta.pacienteId)
                .FirstOrDefaultAsync();

            ViewBag.MedicoNombre   = medico?.nombre  ?? "—";
            ViewBag.PacienteNombre = paciente?.nombre ?? "—";
            ViewBag.MedicoFoto     = medico?.foto ?? "";
            ViewBag.PacienteFoto   = paciente?.foto ?? "";

            return View(consulta);
        }

        // POST: Consultas/Atender/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atender(string id, string diagnostico)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
                return NotFound();

            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                ModelState.AddModelError("diagnostico", "El diagnóstico no puede estar vacío.");
                var consulta = await _context.Consultas.Find(c => c._id == id).FirstOrDefaultAsync();
                var medico   = await _context.Medicos.Find(m => m._id == consulta!.medicoId).FirstOrDefaultAsync();
                var paciente = await _context.Pacientes.Find(p => p._id == consulta!.pacienteId).FirstOrDefaultAsync();
                ViewBag.MedicoNombre   = medico?.nombre  ?? "—";
                ViewBag.PacienteNombre = paciente?.nombre ?? "—";
                ViewBag.MedicoFoto     = medico?.foto ?? "";
                ViewBag.PacienteFoto   = paciente?.foto ?? "";
                return View(consulta);
            }

            var update = Builders<Consultas>.Update.Set(c => c.diagnostico, diagnostico.Trim());
            await _context.Consultas.UpdateOneAsync(c => c._id == id, update);

            return RedirectToAction(nameof(Index));
        }

        // GET: Consultas/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var consultas = await _context.Consultas
                .Find(c => c._id == id)
                .FirstOrDefaultAsync();

            if (consultas == null)
            {
                return NotFound();
            }

            return View(consultas);
        }

        // POST: Consultas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Consultas
                .DeleteOneAsync(c => c._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarSelects(string? selectedMedicoId = null, string? selectedPacienteId = null)
        {
            var medicos = await _context.Medicos
                .Find(Builders<Medicos>.Filter.Empty)
                .ToListAsync();

            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .ToListAsync();

            ViewBag.Medicos = new SelectList(medicos, "_id", "nombre", selectedMedicoId);
            ViewBag.Pacientes = new SelectList(pacientes, "_id", "nombre", selectedPacienteId);
        }
    }
}
