using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using Neflis.Models;
using Neflis.Models.ViewModels;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class PerfilesController : Controller
    {
        private readonly NeflisDbContext _context;

        public PerfilesController(NeflisDbContext context)
        {
            _context = context;
        }

        // helper para sacar el id del usuario logueado
        private int GetUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(claim!.Value);
        }

        // ya no queremos el listado plano -> mandamos al selector
        public IActionResult Index()
        {
            return RedirectToAction("Index", "PerfilSelector");
        }

        // GET: Perfiles/Create
        public IActionResult Create()
        {
            var usuarioId = GetUsuarioId();

            // validar suscripción activa
            var suscripcion = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId && s.Estado == "Activa")
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            if (suscripcion == null)
            {
                TempData["MensajeError"] = "No tienes una suscripción activa. Por favor elige un plan antes de crear perfiles.";
                return RedirectToAction("Planes", "Suscripciones");
            }

            return View(new PerfilViewModel());
        }

        // POST: Perfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PerfilViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuarioId = GetUsuarioId();

            // validar suscripción activa
            var suscripcion = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId && s.Estado == "Activa")
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            if (suscripcion == null)
            {
                TempData["MensajeError"] = "No tienes una suscripción activa. Por favor elige un plan antes de crear perfiles.";
                return RedirectToAction("Planes", "Suscripciones");
            }

            // limitar por plan
            var plan = _context.PlanesSuscripcion
                .FirstOrDefault(p => p.PlanSuscripcionId == suscripcion.PlanSuscripcionId);

            int maxPerfilesPermitidos = plan?.MaxPerfiles ?? 2;

            var perfilesActuales = _context.Perfiles.Count(p => p.UsuarioId == usuarioId);
            if (perfilesActuales >= maxPerfilesPermitidos)
            {
                ModelState.AddModelError("", $"Ya alcanzaste el máximo de perfiles permitidos ({maxPerfilesPermitidos}).");
                return View(model);
            }

            var perfil = new Perfil
            {
                UsuarioId = usuarioId,
                NombrePerfil = model.NombrePerfil,
                EsInfantil = model.EsInfantil,
                AvatarUrl = model.AvatarUrl
            };

            _context.Perfiles.Add(perfil);
            _context.SaveChanges();

            // 👇 después de crear volvemos al selector
            return RedirectToAction("Index", "PerfilSelector");
        }

        // GET: Perfiles/Edit/5  (esto lo dejamos por si quieres editar desde otro lado)
        public IActionResult Edit(int id)
        {
            var usuarioId = GetUsuarioId();
            var perfil = _context.Perfiles.FirstOrDefault(p => p.PerfilId == id && p.UsuarioId == usuarioId);
            if (perfil == null)
                return NotFound();

            var vm = new PerfilViewModel
            {
                PerfilId = perfil.PerfilId,
                NombrePerfil = perfil.NombrePerfil,
                EsInfantil = perfil.EsInfantil,
                AvatarUrl = perfil.AvatarUrl
            };

            return View(vm);
        }

        // POST: Perfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, PerfilViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuarioId = GetUsuarioId();
            var perfil = _context.Perfiles.FirstOrDefault(p => p.PerfilId == id && p.UsuarioId == usuarioId);
            if (perfil == null)
                return NotFound();

            perfil.NombrePerfil = model.NombrePerfil;
            perfil.EsInfantil = model.EsInfantil;
            perfil.AvatarUrl = model.AvatarUrl;

            _context.SaveChanges();

            return RedirectToAction("Index", "PerfilSelector");
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var usuarioId = GetUsuarioId();
            var perfil = _context.Perfiles.FirstOrDefault(p => p.PerfilId == id && p.UsuarioId == usuarioId);
            if (perfil != null)
            {
                _context.Perfiles.Remove(perfil);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "PerfilSelector");
        }
    }
}
