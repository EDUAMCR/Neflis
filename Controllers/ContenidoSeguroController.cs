using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using System.Linq;

namespace Neflis.Controllers
{
    [Authorize]
    public class ContenidoSeguroController : Controller
    {
        private readonly NeflisDbContext _context;

        public ContenidoSeguroController(NeflisDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string password)
        {
            // 1. Tomar el ID del usuario que está logueado (del claim)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["MensajeError"] = "Debe iniciar sesión.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Convertirlo a int
            int userId = int.Parse(userIdClaim.Value);

            // 3. Buscar al usuario en TU tabla Usuarios por ID
            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == userId);

            // 4. Validar contraseña (la misma que usó para login)
            if (usuario != null && usuario.Password == password)
            {
                HttpContext.Session.SetString("ContenidoDesbloqueado", "1");
                TempData["Mensaje"] = "Contenido restringido habilitado.";
                return RedirectToAction("Index", "Catalogo");
            }

            TempData["MensajeError"] = "Usuario no encontrado o contraseña incorrecta.";
            return View();
        }

        public IActionResult Bloquear()
        {
            HttpContext.Session.Remove("ContenidoDesbloqueado");
            TempData["Mensaje"] = "Contenido restringido bloqueado.";
            return RedirectToAction("Index", "Catalogo");
        }
    }
}
