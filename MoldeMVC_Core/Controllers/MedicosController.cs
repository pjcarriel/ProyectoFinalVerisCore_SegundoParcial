using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class MedicosController : Controller
    {
        private readonly VerisMongoContext _context;
        private readonly IWebHostEnvironment _env;

        public MedicosController(VerisMongoContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Medicos
        public async Task<IActionResult> Index()
        {
            var medicos = await _context.Medicos
                .Find(Builders<Medicos>.Filter.Empty)
                .ToListAsync();

            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            ViewBag.EspecialidadesDict = especialidades.ToDictionary(e => e._id, e => e.descripcion);

            return View(medicos);
        }

        // GET: Medicos/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicos = await _context.Medicos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicos == null)
            {
                return NotFound();
            }

            var especialidad = await _context.Especialidades
                .Find(e => e._id == medicos.especialidadId)
                .FirstOrDefaultAsync();

            ViewBag.EspecialidadNombre = especialidad?.descripcion ?? medicos.especialidadId;

            return View(medicos);
        }

        // GET: Medicos/Create
        public async Task<IActionResult> Create()
        {
            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            ViewBag.Especialidades = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                especialidades, "_id", "descripcion");

            CargarFotosMedicos();

            return View();
        }

        // POST: Medicos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("nombre,especialidadId,foto")] Medicos medicos)
        {
            medicos._id = ObjectId.GenerateNewId().ToString();

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }

            if (!ObjectId.TryParse(medicos.especialidadId, out _))
            {
                ModelState.AddModelError("especialidadId", "La especialidad seleccionada no es válida.");
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }

            try
            {
                await _context.Medicos.InsertOneAsync(medicos);

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al guardar en MongoDB: " + ex.Message);
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }
        }

        // GET: Medicos/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicos = await _context.Medicos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicos == null)
            {
                return NotFound();
            }

            await RecargarEspecialidades(medicos.especialidadId);
            CargarFotosMedicos();

            return View(medicos);
        }

        // POST: Medicos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,nombre,especialidadId,foto")] Medicos medicos)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != medicos._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }

            if (!ObjectId.TryParse(medicos.especialidadId, out _))
            {
                ModelState.AddModelError("especialidadId", "La especialidad seleccionada no es válida.");
                await RecargarEspecialidades(medicos.especialidadId);
                CargarFotosMedicos();
                return View(medicos);
            }

            try
            {
                var resultado = await _context.Medicos
                    .ReplaceOneAsync(m => m._id == id, medicos);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                await RecargarEspecialidades(medicos.especialidadId);
                return View(medicos);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await RecargarEspecialidades(medicos.especialidadId);
                return View(medicos);
            }
        }

        // GET: Medicos/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicos = await _context.Medicos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicos == null)
            {
                return NotFound();
            }

            return View(medicos);
        }

        // POST: Medicos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Medicos
                .DeleteOneAsync(m => m._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarEspecialidades(string? selectedId = null)
        {
            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            ViewBag.Especialidades = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                especialidades, "_id", "descripcion", selectedId);
        }

        private void CargarFotosMedicos()
        {
            var dir = Path.Combine(_env.WebRootPath, "medicos");
            ViewBag.Fotos = Directory.Exists(dir)
                ? Directory.GetFiles(dir)
                    .Select(Path.GetFileName)
                    .OrderBy(f => f)
                    .ToList()
                : new List<string>();
        }
    }
}