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
    [Authorize(Roles = "Administrador,SuperAdmin,Paciente")]
    public class PacientesController : Controller
    {
        private readonly MongoDbContext            _mongo;
        private readonly IWebHostEnvironment       _env;
        private readonly UserManager<IdentityUser> _userManager;

        public PacientesController(MongoDbContext mongo, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _mongo       = mongo;
            _env         = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            List<Pacientes> pacientes;

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (!int.TryParse(objUser?.PhoneNumber, out var cedula))
                    return View(new List<Pacientes>());
                pacientes = await _mongo.Pacientes
                    .Find(p => p.Cedula == cedula)
                    .ToListAsync();
            }
            else
            {
                pacientes = await _mongo.Pacientes.Find(_ => true).ToListAsync();
            }

            return View(pacientes);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var paciente = await _mongo.Pacientes.Find(p => p.Id == oid).FirstOrDefaultAsync();
            if (paciente == null) return NotFound();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                if (paciente.Cedula.ToString() != objUser?.PhoneNumber) return Forbid();
            }

            return View(paciente);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            CargarFotosPacientes();
            await CargarUsuarios();
            return View();
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Pacientes paciente, string? usuarioId)
        {
            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                await CargarUsuarios(usuarioId);
                return View(paciente);
            }

            var existeCedula = await _mongo.Pacientes.CountDocumentsAsync(p => p.Cedula == paciente.Cedula) > 0;
            if (existeCedula)
            {
                ModelState.AddModelError("Cedula", "Ya existe un paciente registrado con esta cédula.");
                CargarFotosPacientes();
                await CargarUsuarios(usuarioId);
                return View(paciente);
            }

            try
            {
                await _mongo.Pacientes.InsertOneAsync(paciente);

                // Actualizar PhoneNumber del usuario Identity con la cédula
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    var user = await _userManager.FindByIdAsync(usuarioId);
                    if (user != null)
                    {
                        user.PhoneNumber = paciente.Cedula.ToString();
                        await _userManager.UpdateAsync(user);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                await CargarUsuarios(usuarioId);
                return View(paciente);
            }
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var paciente = await _mongo.Pacientes.Find(p => p.Id == oid).FirstOrDefaultAsync();
            if (paciente == null) return NotFound();

            CargarFotosPacientes();
            return View(paciente);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Pacientes paciente)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                return View(paciente);
            }

            var cedulaUsadaPorOtro = await _mongo.Pacientes
                .CountDocumentsAsync(p => p.Cedula == paciente.Cedula && p.Id != oid) > 0;

            if (cedulaUsadaPorOtro)
            {
                ModelState.AddModelError("Cedula", "Ya existe otro paciente registrado con esta cédula.");
                CargarFotosPacientes();
                return View(paciente);
            }

            try
            {
                // Si cambió la cédula, actualizar PhoneNumber del usuario Identity vinculado
                var existing = await _mongo.Pacientes.Find(p => p.Id == oid).FirstOrDefaultAsync();
                if (existing != null && existing.Cedula != paciente.Cedula)
                {
                    var oldCedStr = existing.Cedula.ToString();
                    var user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == oldCedStr);
                    if (user != null)
                    {
                        user.PhoneNumber = paciente.Cedula.ToString();
                        await _userManager.UpdateAsync(user);
                    }
                }

                paciente.Id = oid;
                await _mongo.Pacientes.ReplaceOneAsync(p => p.Id == oid, paciente);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                return View(paciente);
            }
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            var paciente = await _mongo.Pacientes.Find(p => p.Id == oid).FirstOrDefaultAsync();
            if (paciente == null) return NotFound();

            return View(paciente);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            // Eliminar recetas y consultas del paciente antes de borrarlo
            var pacienteIdStr = oid.ToString();
            var consultas = await _mongo.Consultas.Find(c => c.PacienteId == pacienteIdStr).ToListAsync();
            foreach (var consulta in consultas)
            {
                var consultaIdStr = consulta.Id.ToString();
                await _mongo.Recetas.DeleteManyAsync(r => r.ConsultaId == consultaIdStr);
                await _mongo.Consultas.DeleteOneAsync(c => c.Id == consulta.Id);
            }

            await _mongo.Pacientes.DeleteOneAsync(p => p.Id == oid);
            return RedirectToAction(nameof(Index));
        }

        private void CargarFotosPacientes()
        {
            var dir = Path.Combine(_env.WebRootPath, "Usuarios");
            ViewBag.Fotos = Directory.Exists(dir)
                ? Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(f => f).ToList()
                : new List<string>();
        }

        private async Task CargarUsuarios(string? selectedId = null)
        {
            var usuarios = (await _userManager.GetUsersInRoleAsync("Paciente"))
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName })
                .ToList();
            ViewBag.Usuarios = new SelectList(usuarios, "Id", "UserName", selectedId);
        }
    }
}
