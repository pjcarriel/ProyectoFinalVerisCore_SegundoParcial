using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin")]
    public class MedicamentosController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;

        public MedicamentosController(ProyectoVerisMvcBdContext context)
        {
            _context = context;
        }

        // GET: Medicamentos
        public async Task<IActionResult> Index()
        {
            var medicamentos = await _context.Medicamentos.ToListAsync();
            return View(medicamentos);
        }

        // GET: Medicamentos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        // GET: Medicamentos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Medicamentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Tipo")] Medicamento medicamento)
        {
            if (!ModelState.IsValid)
                return View(medicamento);

            try
            {
                _context.Add(medicamento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamento);
            }
        }

        // GET: Medicamentos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        // POST: Medicamentos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdMedicamento,Nombre,Tipo")] Medicamento medicamento)
        {
            if (id != medicamento.IdMedicamento)
                return NotFound();

            if (!ModelState.IsValid)
                return View(medicamento);

            try
            {
                _context.Update(medicamento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Medicamentos.AnyAsync(m => m.IdMedicamento == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(medicamento);
            }
        }

        // GET: Medicamentos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null)
                return NotFound();

            return View(medicamento);
        }

        // POST: Medicamentos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null)
                return NotFound();

            _context.Remove(medicamento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
