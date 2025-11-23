using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class HistorialPagosController : Controller
    {
        private readonly NeflisDbContext _context;

        public HistorialPagosController(NeflisDbContext context)
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

            var pagos = _context.HistorialPagos
                .Where(h => h.UsuarioId == usuarioId)
                .OrderByDescending(h => h.FechaPago)
                .ToList();

            return View(pagos);
        }
    }
}
