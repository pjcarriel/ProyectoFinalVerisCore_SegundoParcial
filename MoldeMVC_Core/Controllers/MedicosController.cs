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
                    .Find(m => m.IdUsuario == objUser!.Id)
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
                if (medico.IdUsuario != objUser?.Id) return Forbid();
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
            CargarUsuarios();
            return View();
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,EspecialidadId,Foto")] Medicos medico)
        {
            ModelState.Remove("EspecialidadNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            var esPaciente = await _mongo.Pacientes.CountDocumentsAsync(p => p.IdUsuario == medico.IdUsuario) > 0;
            if (esPaciente)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya está registrado como Paciente.");
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            var yaesMedico = await _mongo.Medicos.CountDocumentsAsync(m => m.IdUsuario == medico.IdUsuario) > 0;
            if (yaesMedico)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya tiene un registro de Médico.");
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
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
                CargarUsuarios(medico.IdUsuario);
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
            CargarUsuarios(medico.IdUsuario);
            return View(medico);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("IdUsuario,Nombre,EspecialidadId,Foto")] Medicos medico)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            ModelState.Remove("EspecialidadNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.EspecialidadId);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
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
                CargarUsuarios(medico.IdUsuario);
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

        private void CargarUsuarios(string? selectedId = null)
        {
            var usuarios = _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName })
                .ToList();
            ViewBag.Usuarios = new SelectList(usuarios, "Id", "UserName", selectedId);
        }
    }
}
