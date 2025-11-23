using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class PerfilSelectorController : Controller
    {
        private readonly NeflisDbContext _context;

        public PerfilSelectorController(NeflisDbContext context)
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

            // perfiles del usuario
            var perfiles = _context.Perfiles
                .Where(p => p.UsuarioId == usuarioId)
                .ToList();

            // suscripción activa
            var susActiva = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId && s.Estado == "Activa")
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            int maxPerfiles = 1; // por si no tiene plan

            if (susActiva != null)
            {
                var plan = _context.PlanesSuscripcion
                    .FirstOrDefault(p => p.PlanSuscripcionId == susActiva.PlanSuscripcionId);

                if (plan != null)
                    maxPerfiles = plan.MaxPerfiles;
            }

            ViewBag.MaxPerfiles = maxPerfiles;

            return View(perfiles);
        }


        // /PerfilSelector/Usar/5
        public IActionResult Usar(int id)
        {
            var usuarioId = GetUsuarioId();
            var perfil = _context.Perfiles.FirstOrDefault(p => p.PerfilId == id && p.UsuarioId == usuarioId);
            if (perfil == null)
                return NotFound();

            // guardamos en sesión
            HttpContext.Session.SetInt32("PerfilIdActual", perfil.PerfilId);
            HttpContext.Session.SetString("PerfilEsInfantil", perfil.EsInfantil ? "1" : "0");

            return RedirectToAction("Index", "Catalogo");
        }
    }
}
