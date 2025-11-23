using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly NeflisDbContext _context;

        public NotificacionesController(NeflisDbContext context)
        {
            _context = context;
        }

        private int GetUsuarioId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        public IActionResult Index()
        {
            var usuarioId = GetUsuarioId();
            var notis = _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.Fecha)
                .ToList();

            return View(notis);
        }
    }
}
