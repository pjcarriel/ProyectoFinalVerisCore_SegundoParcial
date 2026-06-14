
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin,Paciente")]
    public class PacientesController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;
        private readonly IWebHostEnvironment        _env;
        private readonly UserManager<IdentityUser>  _userManager;

        public PacientesController(ProyectoVerisMvcBdContext context, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _context     = context;
            _env         = env;
            _userManager = userManager;
        }

        // GET: Pacientes
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                var misPacientes = await _context.Pacientes
                    .Where(p => p.IdUsuario == userId)
                    .ToListAsync();
                return View(misPacientes);
            }
            var pacientes = await _context.Pacientes.ToListAsync();
            return View(pacientes);
        }

        // GET: Pacientes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (paciente == null)
                return NotFound();

            if (User.IsInRole("Paciente"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                if (paciente.IdUsuario != userId)
                    return Forbid();
            }

            return View(paciente);
        }

        // GET: Pacientes/Create
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public IActionResult Create()
        {
            CargarFotosPacientes();
            CargarUsuarios();
            return View();
        }

        // POST: Pacientes/Create
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Paciente paciente)
        {
            ModelState.Remove("IdUsuarioNavigation");

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var esMedico = await _context.Medicos.AnyAsync(m => m.IdUsuario == paciente.IdUsuario);
            if (esMedico)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya está registrado como Médico. Un usuario no puede tener ambos roles.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var yaEsPaciente = await _context.Pacientes.AnyAsync(p => p.IdUsuario == paciente.IdUsuario);
            if (yaEsPaciente)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya tiene un registro de Paciente.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var existeCedula = await _context.Pacientes.AnyAsync(p => p.Cedula == paciente.Cedula);
            if (existeCedula)
            {
                ModelState.AddModelError("Cedula", "Ya existe un paciente registrado con esta cédula.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            try
            {
                await SincronizarUsuarioLegacy(paciente.IdUsuario);
                _context.Add(paciente);
                await _context.SaveChangesAsync();
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

        // GET: Pacientes/Edit/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (paciente == null)
                return NotFound();

            CargarFotosPacientes();
            CargarUsuarios(paciente.IdUsuario);
            return View(paciente);
        }

        // POST: Pacientes/Edit/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] Paciente paciente)
        {
            if (id != paciente.IdPaciente)
                return NotFound();

            ModelState.Remove("IdUsuarioNavigation");

            if (!ModelState.IsValid)
            {
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            var cedulaUsadaPorOtro = await _context.Pacientes
                .AnyAsync(p => p.Cedula == paciente.Cedula && p.IdPaciente != paciente.IdPaciente);

            if (cedulaUsadaPorOtro)
            {
                ModelState.AddModelError("Cedula", "Ya existe otro paciente registrado con esta cédula.");
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }

            try
            {
                await SincronizarUsuarioLegacy(paciente.IdUsuario);
                _context.Update(paciente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Pacientes.AnyAsync(p => p.IdPaciente == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                CargarFotosPacientes();
                CargarUsuarios(paciente.IdUsuario);
                return View(paciente);
            }
        }

        // GET: Pacientes/Delete/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (paciente == null)
                return NotFound();

            return View(paciente);
        }

        // POST: Pacientes/Delete/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (paciente == null)
                return NotFound();

            // Eliminar recetas y consultas asociadas antes de borrar el paciente
            // (FK constraints impiden borrar directamente si tiene consultas)
            var consultas = await _context.Consultas
                .Include(c => c.Receta)
                .Where(c => c.IdPaciente == id)
                .ToListAsync();

            foreach (var consulta in consultas)
            {
                _context.RemoveRange(consulta.Receta);
                _context.Remove(consulta);
            }

            _context.Remove(paciente);
            await _context.SaveChangesAsync();
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

        private void CargarUsuarios(string? selectedId = null)
        {
            var usuarios = _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName })
                .ToList();

            ViewBag.Usuarios = new SelectList(usuarios, "Id", "UserName", selectedId);
        }

        // Sincroniza el usuario de BDNetCore_Identity en la tabla legacy de ProyectoVeris_MVC_BD
        // para satisfacer la FK constraint FK_pacientes_AspNetUsers
        private async Task SincronizarUsuarioLegacy(string userId)
        {
            var existe = await _context.AspNetUsers.AnyAsync(u => u.Id == userId);
            if (!existe)
            {
                var identityUser = await _userManager.FindByIdAsync(userId);
                if (identityUser != null)
                {
                    _context.AspNetUsers.Add(new AspNetUser
                    {
                        Id                  = identityUser.Id,
                        UserName            = identityUser.UserName ?? identityUser.Email ?? userId,
                        Email               = identityUser.Email,
                        EmailConfirmed      = true,
                        PasswordHash        = null,
                        SecurityStamp       = null,
                        PhoneNumberConfirmed = false,
                        TwoFactorEnabled    = false,
                        LockoutEnabled      = false,
                        AccessFailedCount   = 0
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
