using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Neflis.Controllers
{
    public class HomeController : Controller
    {
        // Página principal
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Si ya está autenticado, lo redirigimos al catálogo
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Catalogo");
            }

            return View();
        }

        // Información general o “Acerca de”
        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Remove("ContenidoDesbloqueado");

            return RedirectToAction("Login", "Account");
        }

        // Error general (fallback)
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
