using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;
using System.Text.Json;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "Administrador,SuperAdmin,Medico")]
    public class MedicosController : Controller
    {
        private readonly ProyectoVerisMvcBdContext _context;
        private readonly IWebHostEnvironment        _env;
        private readonly UserManager<IdentityUser>  _userManager;

        public MedicosController(ProyectoVerisMvcBdContext context, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _context     = context;
            _env         = env;
            _userManager = userManager;
        }

        // GET: Medicos
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Medico"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                var misMedicos = await _context.Medicos
                    .Include(m => m.IdEspecialidadNavigation)
                    .Where(m => m.IdUsuario == userId)
                    .ToListAsync();
                return View(misMedicos);
            }

            var medicos = await _context.Medicos
                .Include(m => m.IdEspecialidadNavigation)
                .ToListAsync();

            return View(medicos);
        }

        // GET: Medicos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var medico = await _context.Medicos
                .Include(m => m.IdEspecialidadNavigation)
                .FirstOrDefaultAsync(m => m.IdMedico == id);

            if (medico == null)
                return NotFound();

            if (User.IsInRole("Medico"))
            {
                var sesion = HttpContext.Session.GetString("User");
                var objUser = JsonSerializer.Deserialize<IdentityUser>(sesion!);
                var userId = objUser?.Id;
                if (medico.IdUsuario != userId)
                    return Forbid();
            }

            ViewBag.EspecialidadNombre = medico.IdEspecialidadNavigation?.Descripcion ?? "";

            return View(medico);
        }

        // GET: Medicos/Create
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await CargarEspecialidades();
            CargarFotosMedicos();
            CargarUsuarios();
            return View();
        }

        // POST: Medicos/Create
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,IdEspecialidad,Foto")] Medico medico)
        {
            ModelState.Remove("IdEspecialidadNavigation");
            ModelState.Remove("IdUsuarioNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            var esPaciente = await _context.Pacientes.AnyAsync(p => p.IdUsuario == medico.IdUsuario);
            if (esPaciente)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya está registrado como Paciente. Un usuario no puede tener ambos roles.");
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            var yaesMedico = await _context.Medicos.AnyAsync(m => m.IdUsuario == medico.IdUsuario);
            if (yaesMedico)
            {
                ModelState.AddModelError("IdUsuario", "Este usuario ya tiene un registro de Médico.");
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            try
            {
                await SincronizarUsuarioLegacy(medico.IdUsuario);
                _context.Add(medico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }
        }

        // GET: Medicos/Edit/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var medico = await _context.Medicos
                .FirstOrDefaultAsync(m => m.IdMedico == id);

            if (medico == null)
                return NotFound();

            await CargarEspecialidades(medico.IdEspecialidad);
            CargarFotosMedicos();
            CargarUsuarios(medico.IdUsuario);
            return View(medico);
        }

        // POST: Medicos/Edit/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdMedico,IdUsuario,Nombre,IdEspecialidad,Foto")] Medico medico)
        {
            if (id != medico.IdMedico)
                return NotFound();

            ModelState.Remove("IdEspecialidadNavigation");
            ModelState.Remove("IdUsuarioNavigation");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }

            try
            {
                await SincronizarUsuarioLegacy(medico.IdUsuario);
                _context.Update(medico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Medicos.AnyAsync(m => m.IdMedico == id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                await CargarEspecialidades(medico.IdEspecialidad);
                CargarFotosMedicos();
                CargarUsuarios(medico.IdUsuario);
                return View(medico);
            }
        }

        // GET: Medicos/Delete/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var medico = await _context.Medicos
                .Include(m => m.IdEspecialidadNavigation)
                .FirstOrDefaultAsync(m => m.IdMedico == id);

            if (medico == null)
                return NotFound();

            return View(medico);
        }

        // POST: Medicos/Delete/5
        [Authorize(Roles = "Administrador,SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medico = await _context.Medicos
                .FirstOrDefaultAsync(m => m.IdMedico == id);

            if (medico == null)
                return NotFound();

            _context.Remove(medico);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarEspecialidades(int? selectedId = null)
        {
            var especialidades = await _context.Especialidades.ToListAsync();
            ViewBag.Especialidades = new SelectList(especialidades, "IdEspecialidad", "Descripcion", selectedId);
        }

        private void CargarFotosMedicos()
        {
            var dir = Path.Combine(_env.WebRootPath, "medicos");
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
        // para satisfacer la FK constraint FK_medicos_AspNetUsers
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
                        Id                   = identityUser.Id,
                        UserName             = identityUser.UserName ?? identityUser.Email ?? userId,
                        Email                = identityUser.Email,
                        EmailConfirmed       = true,
                        PasswordHash         = null,
                        SecurityStamp        = null,
                        PhoneNumberConfirmed = false,
                        TwoFactorEnabled     = false,
                        LockoutEnabled       = false,
                        AccessFailedCount    = 0
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
