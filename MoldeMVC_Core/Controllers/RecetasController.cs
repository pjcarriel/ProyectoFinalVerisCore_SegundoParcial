using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class RecetasController : Controller
    {
        private readonly VerisMongoContext _context;

        public RecetasController(VerisMongoContext context)
        {
            _context = context;
        }

        // GET: Recetas
        public async Task<IActionResult> Index()
        {
            var recetas = await _context.Recetas
                .Find(Builders<Recetas>.Filter.Empty)
                .ToListAsync();

            var consultas = await _context.Consultas
                .Find(Builders<Consultas>.Filter.Empty)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Find(Builders<Medicamentos>.Filter.Empty)
                .ToListAsync();

            ViewBag.ConsultasDict = consultas.ToDictionary(c => c._id, c => c.diagnostico);

            ViewBag.MedicamentosDict = medicamentos.ToDictionary(m => m._id, m => m.nombre);

            return View(recetas);
        }

        // GET: Recetas/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var recetas = await _context.Recetas
                .Find(r => r._id == id)
                .FirstOrDefaultAsync();

            if (recetas == null)
            {
                return NotFound();
            }

            var consulta = await _context.Consultas
                .Find(c => c._id == recetas.consultaId)
                .FirstOrDefaultAsync();

            var medicamento = await _context.Medicamentos
                .Find(m => m._id == recetas.medicamentoId)
                .FirstOrDefaultAsync();

            ViewBag.ConsultaNombre = consulta?.diagnostico ?? recetas.consultaId;

            ViewBag.MedicamentoNombre = medicamento?.nombre ?? recetas.medicamentoId;

            return View(recetas);
        }

        // GET: Recetas/Create
        public async Task<IActionResult> Create()
        {
            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .SortBy(p => p.nombre)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Find(Builders<Medicamentos>.Filter.Empty)
                .SortBy(m => m.nombre)
                .ToListAsync();

            ViewBag.PacientesList = pacientes;
            ViewBag.Medicamentos = new SelectList(medicamentos, "_id", "nombre");
            return View();
        }

        // AJAX: consultas de un paciente
        [HttpGet]
        public async Task<IActionResult> GetConsultasPaciente(string pacienteId)
        {
            var consultas = await _context.Consultas
                .Find(c => c.pacienteId == pacienteId)
                .SortByDescending(c => c.fechaConsulta)
                .ToListAsync();

            var medicos = await _context.Medicos
                .Find(Builders<Medicos>.Filter.Empty)
                .ToListAsync();

            var medicosDict = medicos.ToDictionary(m => m._id, m => m.nombre);

            var resultado = consultas.Select(c => new
            {
                id = c._id,
                texto = $"{c.fechaConsulta:dd/MM/yyyy}  {c.hi}–{c.hf}  |  {(medicosDict.ContainsKey(c.medicoId) ? medicosDict[c.medicoId] : "?")}",
                diagnostico = c.diagnostico ?? "Pendiente",
                pendiente = string.IsNullOrWhiteSpace(c.diagnostico) || c.diagnostico.Trim().ToLower() == "pendiente"
            });

            return Json(resultado);
        }

        // AJAX: diagnóstico de una consulta
        [HttpGet]
        public async Task<IActionResult> GetDiagnostico(string consultaId)
        {
            var consulta = await _context.Consultas
                .Find(c => c._id == consultaId)
                .FirstOrDefaultAsync();

            if (consulta == null)
                return Json(new { diagnostico = "", pendiente = true });

            bool pendiente = string.IsNullOrWhiteSpace(consulta.diagnostico) ||
                             consulta.diagnostico.Trim().ToLower() == "pendiente";

            return Json(new { diagnostico = consulta.diagnostico ?? "Pendiente", pendiente });
        }

        // POST: Recetas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("consultaId,medicamentoId,cantidad")] Recetas recetas)
        {
            recetas._id = ObjectId.GenerateNewId().ToString();
            ModelState.Remove("_id");

            // Validar que la consulta tenga diagnóstico
            if (!string.IsNullOrWhiteSpace(recetas.consultaId))
            {
                var consulta = await _context.Consultas
                    .Find(c => c._id == recetas.consultaId)
                    .FirstOrDefaultAsync();

                if (consulta == null ||
                    string.IsNullOrWhiteSpace(consulta.diagnostico) ||
                    consulta.diagnostico.Trim().ToLower() == "pendiente")
                {
                    ModelState.AddModelError("", "La consulta seleccionada aún no tiene diagnóstico registrado. Registre el diagnóstico antes de crear la receta.");
                }
            }

            if (!ModelState.IsValid)
            {
                await RecargarCreateSelects(recetas.medicamentoId);
                return View(recetas);
            }

            try
            {
                await _context.Recetas.InsertOneAsync(recetas);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarCreateSelects(recetas.medicamentoId);
                return View(recetas);
            }
        }

        private async Task RecargarCreateSelects(string? selectedMedicamentoId = null)
        {
            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .SortBy(p => p.nombre).ToListAsync();
            var medicamentos = await _context.Medicamentos
                .Find(Builders<Medicamentos>.Filter.Empty)
                .SortBy(m => m.nombre).ToListAsync();
            ViewBag.PacientesList = pacientes;
            ViewBag.Medicamentos = new SelectList(medicamentos, "_id", "nombre", selectedMedicamentoId);
        }

        // GET: Recetas/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var recetas = await _context.Recetas
                .Find(r => r._id == id)
                .FirstOrDefaultAsync();

            if (recetas == null)
            {
                return NotFound();
            }

            await RecargarSelects(recetas.consultaId, recetas.medicamentoId);

            return View(recetas);
        }

        // POST: Recetas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,consultaId,medicamentoId,cantidad")] Recetas recetas)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != recetas._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                await RecargarSelects(recetas.consultaId, recetas.medicamentoId);
                return View(recetas);
            }

            if (!ObjectId.TryParse(recetas.consultaId, out _))
            {
                ModelState.AddModelError("consultaId", "La consulta seleccionada no es válida.");
                await RecargarSelects(recetas.consultaId, recetas.medicamentoId);
                return View(recetas);
            }

            if (!ObjectId.TryParse(recetas.medicamentoId, out _))
            {
                ModelState.AddModelError("medicamentoId", "El medicamento seleccionado no es válido.");
                await RecargarSelects(recetas.consultaId, recetas.medicamentoId);
                return View(recetas);
            }

            try
            {
                var resultado = await _context.Recetas
                    .ReplaceOneAsync(r => r._id == id, recetas);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                await RecargarSelects(recetas.consultaId, recetas.medicamentoId);
                return View(recetas);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarSelects(recetas.consultaId, recetas.medicamentoId);
                return View(recetas);
            }
        }

        // GET: Recetas/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var recetas = await _context.Recetas
                .Find(r => r._id == id)
                .FirstOrDefaultAsync();

            if (recetas == null)
            {
                return NotFound();
            }

            return View(recetas);
        }

        // POST: Recetas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Recetas
                .DeleteOneAsync(r => r._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarSelects(string? selectedConsultaId = null, string? selectedMedicamentoId = null)
        {
            var consultas = await _context.Consultas
                .Find(Builders<Consultas>.Filter.Empty)
                .ToListAsync();

            var medicamentos = await _context.Medicamentos
                .Find(Builders<Medicamentos>.Filter.Empty)
                .ToListAsync();

            ViewBag.Consultas = consultas.Select(c => new SelectListItem
            {
                Value = c._id,
                Text = c.diagnostico,
                Selected = c._id == selectedConsultaId
            }).ToList();

            ViewBag.Medicamentos = new SelectList(medicamentos, "_id", "nombre", selectedMedicamentoId);
        }
    }
}
