using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using Neflis.Models;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class SuscripcionesController : Controller
    {
        private readonly NeflisDbContext _context;

        public SuscripcionesController(NeflisDbContext context)
        {
            _context = context;
        }

        private int GetUsuarioId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // GET: /Suscripciones/Planes
        public IActionResult Planes()
        {
            var planes = _context.PlanesSuscripcion
                .OrderBy(p => p.Precio)
                .ToList();

            return View(planes);
        }

        // POST: /Suscripciones/Contratar
        // ahora NO activa, solo deja PENDIENTE y manda a método de pago si hace falta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contratar(int id)
        {
            var usuarioId = GetUsuarioId();

            var plan = _context.PlanesSuscripcion.Find(id);
            if (plan == null)
                return NotFound();

            // cancelar suscripciones activas o pendientes anteriores
            var anteriores = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId &&
                            (s.Estado == "Activa" || s.Estado == "Pendiente"))
                .ToList();

            foreach (var s in anteriores)
            {
                s.Estado = "Cancelada";
                s.FechaFin = DateTime.UtcNow;
            }

            var nueva = new SuscripcionUsuario
            {
                UsuarioId = usuarioId,
                PlanSuscripcionId = id,
                FechaInicio = DateTime.UtcNow,
                Estado = "Pendiente" // 🔴 antes la ponías "Activa"
            };

            _context.SuscripcionesUsuario.Add(nueva);
            _context.SaveChanges();

            // ¿tiene método de pago?
            bool tieneMetodo = _context.MetodosPago.Any(m => m.UsuarioId == usuarioId);

            if (!tieneMetodo)
            {
                TempData["Mensaje"] = "Antes de activar el plan agregá un método de pago.";
                // lo mandamos a crear método y luego que vuelva a MiPlan
                var returnUrl = Url.Action("MiPlan", "Suscripciones");
                return RedirectToAction("Create", "MetodosPago", new { returnUrl });
            }

            TempData["Mensaje"] = "Plan seleccionado. Confirmá para activarlo.";
            return RedirectToAction("MiPlan");
        }

        // GET: /Suscripciones/MiPlan
        public IActionResult MiPlan()
        {
            var usuarioId = GetUsuarioId();

            var suscripcion = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.FechaInicio)
                .Select(s => new
                {
                    Suscripcion = s,
                    Plan = s.PlanSuscripcion
                })
                .FirstOrDefault();

            if (suscripcion == null)
            {
                TempData["Mensaje"] = "No tienes un plan activo. Elige uno.";
                return RedirectToAction("Planes");
            }

            // para que la vista sepa si mostrar "agregar método"
            var tieneMetodo = _context.MetodosPago.Any(m => m.UsuarioId == usuarioId);
            ViewBag.TieneMetodoPago = tieneMetodo;

            ViewBag.Plan = suscripcion.Plan;
            return View(suscripcion.Suscripcion);
        }

        // POST: /Suscripciones/Confirmar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirmar()
        {
            var usuarioId = GetUsuarioId();

            var suscripcion = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId && s.Estado == "Pendiente")
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            if (suscripcion == null)
            {
                TempData["MensajeError"] = "No hay una suscripción pendiente para activar.";
                return RedirectToAction("MiPlan");
            }

            var plan = _context.PlanesSuscripcion.Find(suscripcion.PlanSuscripcionId);
            if (plan == null)
            {
                TempData["MensajeError"] = "No se encontró el plan asociado.";
                return RedirectToAction("MiPlan");
            }

            var metodo = _context.MetodosPago
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.EsPredeterminado)
                .FirstOrDefault();

            suscripcion.Estado = "Activa";

            _context.HistorialPagos.Add(new HistorialPago
            {
                UsuarioId = usuarioId,
                SuscripcionUsuarioId = suscripcion.SuscripcionUsuarioId,
                Monto = plan.Precio,
                Estado = "Aprobado",
                Metodo = metodo != null ? $"Tarjeta {metodo.NumeroEnmascarado}" : "No definido"
            });

            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId,
                Asunto = "Suscripción activada",
                Mensaje = $"Se activó el plan {plan.NombrePlan}.",
                Fecha = DateTime.UtcNow
            });

            _context.SaveChanges();

            // 👇 después de activar, ver si tiene perfiles
            var tienePerfiles = _context.Perfiles.Any(p => p.UsuarioId == usuarioId);

            if (!tienePerfiles)
            {
                // ir directo a crear el primero
                return RedirectToAction("Create", "Perfiles");
            }

            // si ya tiene al menos uno, lo llevo al selector
            return RedirectToAction("Index", "PerfilSelector");
        }

        // POST: /Suscripciones/Cancelar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar()
        {
            var usuarioId = GetUsuarioId();

            var suscripcion = _context.SuscripcionesUsuario
                .Where(s => s.UsuarioId == usuarioId && s.Estado == "Activa")
                .FirstOrDefault();

            if (suscripcion != null)
            {
                suscripcion.Estado = "Cancelada";
                suscripcion.FechaFin = DateTime.UtcNow;
                _context.SaveChanges();
                TempData["Mensaje"] = "Suscripción cancelada.";
            }

            // 👇 te devuelve a elegir plan
            return RedirectToAction("Planes");
        }
    }
}