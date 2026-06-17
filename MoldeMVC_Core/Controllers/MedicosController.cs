using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using MoldeMVC_Core.Data;
using MoldeMVC_Core.Models;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin,Medico")]
    public class MedicosController : Controller
    {
        private readonly MongoDbContext               _mongo;
        private readonly IWebHostEnvironment          _env;
        private readonly UserManager<IdentityUser>    _userManager;

        public MedicosController(MongoDbContext mongo, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _mongo       = mongo;
            _env         = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            List<Medicos> medicos;

            if (User.IsInRole("Medico"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                medicos = await _mongo.Medicos
                    .Find(m => m.Nombre == objUser!.UserName)
                    .ToListAsync();
            }
            else
            {
                medicos = await _mongo.Medicos.Find(_ => true).ToListAsync();
            }

            await PopularEspecialidades(medicos);
            return View(medicos);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medico = await _mongo.Medicos.Find(m => m.Id == oid).FirstOrDefaultAsync();
            if (medico == null) return NotFound();

            if (User.IsInRole("Medico"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (medico.Nombre != objUser?.UserName) return Forbid();
            }

            medico.EspecialidadNavigation = await _mongo.Especialidades
                .Find(e => e.Id == ObjectId.Parse(medico.EspecialidadId))
                .FirstOrDefaultAsync();

            ViewBag.EspecialidadNombre = medico.EspecialidadNavigation?.Descripcion ?? "";
            return View(medico);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await CargarEspecialidades();
            CargarFotosMedicos();
            return View();
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,EspecialidadId,Foto")] Medicos medico)
        {
            ModelState.Remove("EspecialidadNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                return View(medico);
            }

            // Verificar que el usuario no sea ya paciente (busca por UserName = Nombre)
            var usuarioMedico = await _userManager.FindByNameAsync(medico.Nombre);
            if (usuarioMedico != null && int.TryParse(usuarioMedico.PhoneNumber, out var cedPac))
            {
                var esPaciente = await _mongo.Pacientes.CountDocumentsAsync(p => p.Cedula == cedPac) > 0;
                if (esPaciente)
                {
                    ModelState.AddModelError("Nombre", "El usuario con este nombre ya está registrado como Paciente.");
                    await CargarEspecialidades(medico.EspecialidadId);
                    CargarFotosMedicos();
                    return View(medico);
                }
            }

            var yaesMedico = await _mongo.Medicos.CountDocumentsAsync(m => m.Nombre == medico.Nombre) > 0;
            if (yaesMedico)
            {
                ModelState.AddModelError("Nombre", "Ya existe un médico registrado con este nombre.");
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                return View(medico);
            }

            try
            {
                await _mongo.Medicos.InsertOneAsync(medico);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                return View(medico);
            }
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medico = await _mongo.Medicos.Find(m => m.Id == oid).FirstOrDefaultAsync();
            if (medico == null) return NotFound();

            await CargarEspecialidades(medico.EspecialidadId);
            CargarFotosMedicos();
            return View(medico);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Nombre,EspecialidadId,Foto")] Medicos medico)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            ModelState.Remove("EspecialidadNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                return View(medico);
            }

            try
            {
                medico.Id = oid;
                await _mongo.Medicos.ReplaceOneAsync(m => m.Id == oid, medico);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                return View(medico);
            }
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var medico = await _mongo.Medicos.Find(m => m.Id == oid).FirstOrDefaultAsync();
            if (medico == null) return NotFound();

            medico.EspecialidadNavigation = await _mongo.Especialidades
                .Find(e => e.Id == ObjectId.Parse(medico.EspecialidadId))
                .FirstOrDefaultAsync();

            return View(medico);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            await _mongo.Medicos.DeleteOneAsync(m => m.Id == oid);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopularEspecialidades(List<Medicos> medicos)
        {
            var ids = medicos.Select(m => m.EspecialidadId).Distinct().ToList();
            var oids = ids.Select(ObjectId.Parse).ToList();
            var especialidades = await _mongo.Especialidades
                .Find(e => oids.Contains(e.Id))
                .ToListAsync();
            var dict = especialidades.ToDictionary(e => e.Id.ToString());
            foreach (var m in medicos)
                m.EspecialidadNavigation = dict.GetValueOrDefault(m.EspecialidadId);
        }

        private async Task CargarEspecialidades(string? selectedId = null)
        {
            var especialidades = await _mongo.Especialidades.Find(_ => true).ToListAsync();
            ViewBag.Especialidades = new SelectList(especialidades, "IdStr", "Descripcion", selectedId);
        }

        private void CargarFotosMedicos()
        {
            var dir = Path.Combine(_env.WebRootPath, "medicos");
            ViewBag.Fotos = Directory.Exists(dir)
                ? Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(f => f).ToList()
                : new List<string>();
        }

    }
}
