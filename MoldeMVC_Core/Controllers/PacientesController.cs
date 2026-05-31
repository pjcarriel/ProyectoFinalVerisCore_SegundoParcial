using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class PacientesController : Controller
    {
        private readonly VerisMongoContext _context;
        private readonly IWebHostEnvironment _env;

        public PacientesController(VerisMongoContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Pacientes
        public async Task<IActionResult> Index()
        {
            var pacientes = await _context.Pacientes
                .Find(Builders<Pacientes>.Filter.Empty)
                .ToListAsync();

            return View(pacientes);
        }

        // GET: Pacientes/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var pacientes = await _context.Pacientes
                .Find(p => p._id == id)
                .FirstOrDefaultAsync();

            if (pacientes == null)
            {
                return NotFound();
            }

            return View(pacientes);
        }

        // GET: Pacientes/Create
        public IActionResult Create()
        {
            CargarFotosPacientes();
            return View();
        }

        // POST: Pacientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("nombre,cedula,edad,genero,estatura,peso,foto")] Pacientes pacientes)
        {
            pacientes._id = ObjectId.GenerateNewId().ToString();

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                return View(pacientes);
            }

            try
            {
                var existeCedula = await _context.Pacientes
                    .Find(p => p.cedula == pacientes.cedula)
                    .AnyAsync();

                if (existeCedula)
                {
                    ModelState.AddModelError("cedula", "Ya existe un paciente registrado con esta cédula.");
                    CargarFotosPacientes();
                    return View(pacientes);
                }

                await _context.Pacientes.InsertOneAsync(pacientes);

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al guardar en MongoDB: " + ex.Message);
                CargarFotosPacientes();
                return View(pacientes);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                return View(pacientes);
            }
        }

        // GET: Pacientes/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var pacientes = await _context.Pacientes
                .Find(p => p._id == id)
                .FirstOrDefaultAsync();

            if (pacientes == null)
            {
                return NotFound();
            }

            CargarFotosPacientes();
            return View(pacientes);
        }

        // POST: Pacientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("_id,nombre,cedula,edad,genero,estatura,peso,foto")] Pacientes pacientes)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            if (id != pacientes._id)
            {
                return NotFound();
            }

            ModelState.Remove("_id");

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                return View(pacientes);
            }

            try
            {
                var cedulaUsadaPorOtro = await _context.Pacientes
                    .Find(p => p.cedula == pacientes.cedula && p._id != pacientes._id)
                    .AnyAsync();

                if (cedulaUsadaPorOtro)
                {
                    ModelState.AddModelError("cedula", "Ya existe otro paciente registrado con esta cédula.");
                    CargarFotosPacientes();
                    return View(pacientes);
                }

                var resultado = await _context.Pacientes
                    .ReplaceOneAsync(p => p._id == id, pacientes);

                if (resultado.MatchedCount == 0)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (MongoWriteException ex)
            {
                ModelState.AddModelError("", "Error al actualizar en MongoDB: " + ex.Message);
                return View(pacientes);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(pacientes);
            }
        }

        // GET: Pacientes/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var pacientes = await _context.Pacientes
                .Find(p => p._id == id)
                .FirstOrDefaultAsync();

            if (pacientes == null)
            {
                return NotFound();
            }

            return View(pacientes);
        }

        // POST: Pacientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                return NotFound();
            }

            var resultado = await _context.Pacientes
                .DeleteOneAsync(p => p._id == id);

            if (resultado.DeletedCount == 0)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private void CargarFotosPacientes()
        {
            var dir = Path.Combine(_env.WebRootPath, "Usuarios");
            ViewBag.Fotos = Directory.Exists(dir)
                ? Directory.GetFiles(dir)
                    .Select(Path.GetFileName)
                    .OrderBy(f => f)
                    .ToList()
                : new List<string>();
        }
    }
}
