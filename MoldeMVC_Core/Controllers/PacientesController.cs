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
        private readonly MongoDbContext              _mongo;
        private readonly IWebHostEnvironment         _env;
        private readonly UserManager<IdentityUser>   _userManager;

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
                pacientes = await _mongo.Pacientes
                    .Find(p => p.IdUsuario == objUser!.Id)
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
                if (paciente.IdUsuario != objUser?.Id) return Forbid();
            }

            return View(paciente);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        public IActionResult Create()
        {
            CargarFotosPacientes();
            CargarUsuarios();
            return View();
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Pacientes paciente)
        {
            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var esMedico = await _mongo.Medicos.CountDocumentsAsync(m => m.IdUsuario == paciente.IdUsuario) > 0;
            if (esMedico)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya está registrado como Médico.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var yaEsPaciente = await _mongo.Pacientes.CountDocumentsAsync(p => p.IdUsuario == paciente.IdUsuario) > 0;
            if (yaEsPaciente)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya tiene un registro de Paciente.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var existeCedula = await _mongo.Pacientes.CountDocumentsAsync(p => p.Cedula == paciente.Cedula) > 0;
            if (existeCedula)
            {
                ModelState.AddModelError("Cedula", "Ya existe un paciente registrado con esta cédula.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            try
            {
                await _mongo.Pacientes.InsertOneAsync(paciente);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
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
            CargarUsuarios(paciente.IdUsuario);
            return View(paciente);
        }

        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Pacientes paciente)
        {
            if (!ObjectId.TryParse(id, out var oid))
                return NotFound();

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var cedulaUsadaPorOtro = await _mongo.Pacientes
                .CountDocumentsAsync(p => p.Cedula == paciente.Cedula && p.Id != oid) > 0;

            if (cedulaUsadaPorOtro)
            {
                ModelState.AddModelError("Cedula", "Ya existe otro paciente registrado con esta cédula.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            try
            {
                paciente.Id = oid;
                await _mongo.Pacientes.ReplaceOneAsync(p => p.Id == oid, paciente);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
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
