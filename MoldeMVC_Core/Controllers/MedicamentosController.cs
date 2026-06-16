using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin")]
    public class MedicamentosController : Controller
    {
        private readonly MongoDbContext _mongo;

        public MedicamentosController(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        public async Task<IActionResult> Index()
        {
            var medicamentos = await _mongo.Medicamentos.Find(_ => true).ToListAsync();
            return View(medicamentos);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medicamento = await _mongo.Medicamentos
                .Find(m => m.Id == oid)
                .FirstOrDefaultAsync();

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Tipo")] Medicamentos medicamento)
        {
            if (!ModelState.IsValid)
                return View(medicamento);

            try
            {
                await _mongo.Medicamentos.InsertOneAsync(medicamento);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamento);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medicamento = await _mongo.Medicamentos
                .Find(m => m.Id == oid)
                .FirstOrDefaultAsync();

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Nombre,Tipo")] Medicamentos medicamento)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            if (!ModelState.IsValid)
                return View(medicamento);

            try
            {
                medicamento.Id = oid;
                await _mongo.Medicamentos.ReplaceOneAsync(
                    m => m.Id == oid, medicamento);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamento);
            }
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medicamento = await _mongo.Medicamentos
                .Find(m => m.Id == oid)
                .FirstOrDefaultAsync();

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            await _mongo.Medicamentos.DeleteOneAsync(m => m.Id == oid);
            return RedirectToAction(nameof(Index));
        }
    }
}
