using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin")]
    public class EspecialidadesController : Controller
    {
        private readonly MongoDbContext _mongo;

        public EspecialidadesController(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> Index()
        {
            var especialidades = await _mongo.Especialidades.Find(_ => true).ToListAsync();
            return View(especialidades);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var especialidad = await _mongo.Especialidades
                .Find(e => e.Id == oid)
                .FirstOrDefaultAsync();

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Descripcion,Dias,FranjaHi,FranjaHf")] Especialidades especialidad)
        {
            if (!ModelState.IsValid)
                return View(especialidad);

            try
            {
                await _mongo.Especialidades.InsertOneAsync(especialidad);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidad);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var especialidad = await _mongo.Especialidades
                .Find(e => e.Id == oid)
                .FirstOrDefaultAsync();

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Descripcion,Dias,FranjaHi,FranjaHf")] Especialidades especialidad)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            if (!ModelState.IsValid)
                return View(especialidad);

            try
            {
                especialidad.Id = oid;
                await _mongo.Especialidades.ReplaceOneAsync(
                    e => e.Id == oid, especialidad);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidad);
            }
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var especialidad = await _mongo.Especialidades
                .Find(e => e.Id == oid)
                .FirstOrDefaultAsync();

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            await _mongo.Especialidades.DeleteOneAsync(e => e.Id == oid);
            return RedirectToAction(nameof(Index));
        }
    }
}
