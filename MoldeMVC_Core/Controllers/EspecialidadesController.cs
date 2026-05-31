using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class EspecialidadesController : Controller
    {
        private readonly VerisMongoContext _context;

        public EspecialidadesController(VerisMongoContext context)
        {
            _context = context;
        }

        // GET: Especialidades
        public async Task<IActionResult> Index()
        {
            var especialidades = await _context.Especialidades
                .Find(Builders<Especialidades>.Filter.Empty)
                .ToListAsync();

            return View(especialidades);
        }

        // GET: Especialidades/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var especialidades = await _context.Especialidades
                .Find(e => e._id == id)
                .FirstOrDefaultAsync();

            if (especialidades == null)
            {
                return NotFound();
            }

            return View(especialidades);
        }

        // GET: Especialidades/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Especialidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("descripcion,dias,franjaHI,franjaHF")] Especialidades especialidades)
        {
            especialidades._id = ObjectId.GenerateNewId().ToString();

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                return View(especialidades);
            }

            try
            {
                await _context.Especialidades.InsertOneAsync(especialidades);

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al guardar en MongoDB: " + ex.Message);
                return View(especialidades);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidades);
            }
        }

        // GET: Especialidades/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var especialidades = await _context.Especialidades
                .Find(e => e._id == id)
                .FirstOrDefaultAsync();

            if (especialidades == null)
            {
                return NotFound();
            }

            return View(especialidades);
        }

        // POST: Especialidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,descripcion,dias,franjaHI,franjaHF")] Especialidades especialidades)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != especialidades._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                return View(especialidades);
            }

            try
            {
                var resultado = await _context.Especialidades
                    .ReplaceOneAsync(e => e._id == id, especialidades);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                return View(especialidades);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidades);
            }
        }

        // GET: Especialidades/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var especialidades = await _context.Especialidades
                .Find(e => e._id == id)
                .FirstOrDefaultAsync();

            if (especialidades == null)
            {
                return NotFound();
            }

            return View(especialidades);
        }

        // POST: Especialidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Especialidades
                .DeleteOneAsync(e => e._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}