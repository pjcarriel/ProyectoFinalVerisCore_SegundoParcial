using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin")]
    public class EspecialidadesController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;

        public EspecialidadesController(ProyectoVerisMvcBdContext context)
        {
            _context = context;
        }

        // GET: Especialidades
        public async Task<IActionResult> Index()
        {
            var especialidades = await _context.Especialidades.ToListAsync();
            return View(especialidades);
        }

        // GET: Especialidades/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == id);

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        // GET: Especialidades/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Especialidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Descripcion,Dias,FranjaHi,FranjaHf")] Especialidade especialidad)
        {
            if (!ModelState.IsValid)
                return View(especialidad);

            try
            {
                _context.Add(especialidad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidad);
            }
        }

        // GET: Especialidades/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == id);

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        // POST: Especialidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdEspecialidad,Descripcion,Dias,FranjaHi,FranjaHf")] Especialidade especialidad)
        {
            if (id != especialidad.IdEspecialidad)
                return NotFound();

            if (!ModelState.IsValid)
                return View(especialidad);

            try
            {
                _context.Update(especialidad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Especialidades.AnyAsync(e => e.IdEspecialidad == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(especialidad);
            }
        }

        // GET: Especialidades/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == id);

            if (especialidad == null)
                return NotFound();

            return View(especialidad);
        }

        // POST: Especialidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == id);

            if (especialidad == null)
                return NotFound();

            _context.Remove(especialidad);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
