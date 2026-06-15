#nullable disable

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoldeMVC_Core.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser>   _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser>   userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _logger        = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        // Inicia el challenge hacia el proveedor externo (Facebook)
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);
        }

        // Callback que llama Facebook tras autenticar al usuario
        public async Task<IActionResult> OnGetCallbackAsync(
            string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error del proveedor externo: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "No se pudo obtener información del proveedor externo.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Intenta iniciar sesión con el login externo
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey,
                isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    // Solo el módulo Administrador tiene habilitado el login federado
                    if (!roles.Contains("Administrador"))
                    {
                        await _signInManager.SignOutAsync();
                        ErrorMessage = "El inicio de sesión con Facebook solo está habilitado para el módulo Administrador.";
                        return RedirectToPage("./Login");
                    }

                    _logger.LogInformation("{Name} inició sesión con {Provider}.",
                        info.Principal.Identity?.Name, info.LoginProvider);

                    return LocalRedirect(returnUrl);
                }
            }

            if (result.IsLockedOut)
                return RedirectToPage("./Lockout");

            // No existe cuenta local vinculada — crear nueva cuenta automáticamente
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                     ?? $"{info.ProviderKey}@external.com";

            var userName = (info.Principal.FindFirstValue(ClaimTypes.Name)
                        ?? email.Split('@')[0])
                        .Replace(" ", "_")
                        .Replace("@", "_");

            // Si ya existe un usuario con ese email, solo vincular el proveedor
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                await _userManager.AddLoginAsync(existingUser, info);
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // Crear nuevo usuario con los datos del proveedor externo
            var newUser = new IdentityUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(newUser, info);
                await _userManager.AddToRoleAsync(newUser, "Administrador");
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            ErrorMessage = "No se pudo crear la cuenta. Intente de nuevo.";
            return RedirectToPage("./Login");
        }
    }
}
