using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class MedicamentosController : Controller
    {
        private readonly VerisMongoContext _context;

        public MedicamentosController(VerisMongoContext context)
        {
            _context = context;
        }

        // GET: Medicamentos
        public async Task<IActionResult> Index()
        {
            var medicamentos = await _context.Medicamentos
                .Find(Builders<Medicamentos>.Filter.Empty)
                .ToListAsync();

            return View(medicamentos);
        }

        // GET: Medicamentos/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicamentos = await _context.Medicamentos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicamentos == null)
            {
                return NotFound();
            }

            return View(medicamentos);
        }

        // GET: Medicamentos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Medicamentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("nombre,tipo")] Medicamentos medicamentos)
        {
            medicamentos._id = ObjectId.GenerateNewId().ToString();

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                return View(medicamentos);
            }

            try
            {
                await _context.Medicamentos.InsertOneAsync(medicamentos);

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al guardar en MongoDB: " + ex.Message);
                return View(medicamentos);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamentos);
            }
        }

        // GET: Medicamentos/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicamentos = await _context.Medicamentos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicamentos == null)
            {
                return NotFound();
            }

            return View(medicamentos);
        }

        // POST: Medicamentos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,nombre,tipo")] Medicamentos medicamentos)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != medicamentos._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                return View(medicamentos);
            }

            try
            {
                var resultado = await _context.Medicamentos
                    .ReplaceOneAsync(m => m._id == id, medicamentos);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                return View(medicamentos);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamentos);
            }
        }

        // GET: Medicamentos/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var medicamentos = await _context.Medicamentos
                .Find(m => m._id == id)
                .FirstOrDefaultAsync();

            if (medicamentos == null)
            {
                return NotFound();
            }

            return View(medicamentos);
        }

        // POST: Medicamentos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Medicamentos
                .DeleteOneAsync(m => m._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}