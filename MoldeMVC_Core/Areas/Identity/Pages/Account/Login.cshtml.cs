// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MoldeMVC_Core.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Usuario")]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _signInManager.UserManager.FindByNameAsync(Input.UserName);

                if (user != null && user.UserName == Input.UserName)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        Input.UserName,
                        Input.Password,
                        Input.RememberMe,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");

                        var roles = await _signInManager.UserManager.GetRolesAsync(user);

                        if (!TieneAccesoAlModulo(returnUrl, roles))
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty,
                                "Las credenciales ingresadas no tienen acceso a este módulo.");
                            return Page();
                        }

                        var userJson = System.Text.Json.JsonSerializer.Serialize(user);
                        HttpContext.Session.SetString("User", userJson);

                        if (roles.Contains("SuperAdmin"))    return LocalRedirect("/Rol");
                        if (roles.Contains("Administrador")) return LocalRedirect("/Medicos");
                        if (roles.Contains("Medico"))        return LocalRedirect("/Medicos");
                        if (roles.Contains("Paciente"))      return LocalRedirect("/Pacientes");

                        return LocalRedirect(returnUrl);
                    }

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new
                        {
                            ReturnUrl = returnUrl,
                            RememberMe = Input.RememberMe
                        });
                    }

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }

                    ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
                    return Page();
                }

                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
                return Page();
            }

            return Page();
        }
        private static bool TieneAccesoAlModulo(string returnUrl, IList<string> roles)
        {
            if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/" || returnUrl == "~/")
                return true;

            if (roles.Contains("SuperAdmin"))
                return true;

            var url = returnUrl.ToLower();

            if (url.Contains("/pacientes"))
                return roles.Contains("Paciente") || roles.Contains("Administrador");

            if (url.Contains("/medicos"))
                return roles.Contains("Medico") || roles.Contains("Administrador");

            if (url.Contains("/especialidades") || url.Contains("/medicamentos"))
                return roles.Contains("Administrador");

            if (url.Contains("/consultas") || url.Contains("/recetas") || url.Contains("/agendarconsulta"))
                return roles.Contains("Medico") || roles.Contains("Paciente");

            if (url.Contains("/rol"))
                return false;

            return true;
        }
    }
}