using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class RolController : Controller
    {
        private readonly UserManager<IdentityUser>   _userManager;
        private readonly RoleManager<IdentityRole>   _roleManager;
        private readonly ProyectoVerisMvcBdContext   _context;

        public RolController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ProyectoVerisMvcBdContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context     = context;
        }

        // GET: Rol
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();

            var viewModel = new List<UserRoleViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                viewModel.Add(new UserRoleViewModel
                {
                    Id       = u.Id,
                    UserName = u.UserName ?? "",
                    Email    = u.Email    ?? "",
                    Roles    = string.Join(", ", roles)
                });
            }

            return View(viewModel);
        }

        // GET: Rol/AsignarRol/id
        public async Task<IActionResult> AsignarRol(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var rolesActuales = await _userManager.GetRolesAsync(user);
            ViewBag.UserName   = user.UserName;
            ViewBag.RolActual  = string.Join(", ", rolesActuales);
            ViewBag.Roles      = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());

            return View(user);
        }

        // POST: Rol/AsignarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRol(string id, string rolSeleccionado)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Proteger al SuperAdmin
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["Error"] = "No se pueden modificar los roles del usuario SuperAdmin.";
                return RedirectToAction(nameof(Index));
            }

            // Validar conflicto Médico ↔ Paciente
            if (rolSeleccionado == "Paciente")
            {
                var esMedico = await _context.Medicos.AnyAsync(m => m.IdUsuario == id);
                if (esMedico)
                {
                    TempData["Error"] = $"No se puede asignar el rol Paciente a {user.UserName}: ya tiene un registro de Médico.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (rolSeleccionado == "Medico")
            {
                var esPaciente = await _context.Pacientes.AnyAsync(p => p.IdUsuario == id);
                if (esPaciente)
                {
                    TempData["Error"] = $"No se puede asignar el rol Médico a {user.UserName}: ya tiene un registro de Paciente.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Quitar roles actuales y asignar el nuevo
            var rolesActuales = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, rolesActuales);
            await _userManager.AddToRoleAsync(user, rolSeleccionado);

            TempData["Mensaje"] = $"Rol '{rolSeleccionado}' asignado a {user.UserName}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Rol/QuitarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarRol(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Proteger al SuperAdmin
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["Error"] = "No se pueden modificar los roles del usuario SuperAdmin.";
                return RedirectToAction(nameof(Index));
            }

            var rolesActuales = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, rolesActuales);

            TempData["Mensaje"] = $"Se eliminaron todos los roles del usuario {user.UserName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
