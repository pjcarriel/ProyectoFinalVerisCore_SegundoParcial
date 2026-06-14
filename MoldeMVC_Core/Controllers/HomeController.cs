using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoldeMVC_Core.Models;

namespace MoldeMVC_Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ValidarCedula()
        {
            var usuario = HttpContext.Session.GetString("User");

            var objUser = System.Text.Json.JsonSerializer.Deserialize<IdentityUser>(usuario!);

            var nombre = objUser!.UserName;
            var info   = objUser.PhoneNumber;

            ViewBag.Message = "La cédula de " + nombre + " es: " + info;

            return View("_partial_ValidarCedula", objUser);
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
