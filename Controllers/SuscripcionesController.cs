using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neflis.Data;
using Neflis.Models;
using Neflis.Services;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class SuscripcionesController : Controller
    {
        private readonly NeflisDbContext _context;
        private readonly IEmailService _emailService;

        public SuscripcionesController(NeflisDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetUsuarioId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // --------------------------------------------------------------------
        // LISTA DE PLANES
        // --------------------------------------------------------------------
        public IActionResult Planes()
        {
            var planes = _context.PlanesSuscripcion
                .OrderBy(p => p.Precio)
                .ToList();

            return View(planes);
        }

        // --------------------------------------------------------------------
        // CONTRATAR (DEJA PLAN EN PENDIENTE)
        // --------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contratar(int id)
        {
            var usuarioId = GetUsuarioId();

            var plan = _context.PlanesSuscripcion.Find(id);
            if (plan == null)
                return NotFound();

            // cancelar activas o pendientes anteriores
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
                Estado = "Pendiente"
            };

            _context.SuscripcionesUsuario.Add(nueva);
            _context.SaveChanges();

            bool tieneMetodo = _context.MetodosPago.Any(m => m.UsuarioId == usuarioId);

            if (!tieneMetodo)
            {
                TempData["Mensaje"] = "Antes de activar el plan agregá un método de pago.";
                var returnUrl = Url.Action("MiPlan", "Suscripciones");
                return RedirectToAction("Create", "MetodosPago", new { returnUrl });
            }

            TempData["Mensaje"] = "Plan seleccionado. Confirmá para activarlo.";
            return RedirectToAction("MiPlan");
        }

        // --------------------------------------------------------------------
        // MI PLAN
        // --------------------------------------------------------------------
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

            ViewBag.TieneMetodoPago = _context.MetodosPago.Any(m => m.UsuarioId == usuarioId);
            ViewBag.Plan = suscripcion.Plan;

            return View(suscripcion.Suscripcion);
        }

        // --------------------------------------------------------------------
        // CONFIRMAR SUSCRIPCIÓN (ACTIVA PLAN + ENVÍA CORREO)
        // --------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar()
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

            var plan = await _context.PlanesSuscripcion
                .FirstOrDefaultAsync(p => p.PlanSuscripcionId == suscripcion.PlanSuscripcionId);

            if (plan == null)
            {
                TempData["MensajeError"] = "No se encontró el plan asociado.";
                return RedirectToAction("MiPlan");
            }

            var metodo = _context.MetodosPago
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.EsPredeterminado)
                .FirstOrDefault();

            // ACTIVAR SUSCRIPCIÓN
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

            await _context.SaveChangesAsync();

            // ----------------------------------------------------------------
            // ENVÍO DE CORREO
            // ----------------------------------------------------------------
            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId);

            if (usuario != null)
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var logoUrl = baseUrl + Url.Content("~/img/logo.jpg");

                var asunto = "Neflis - ¡Tu suscripción está activa!";
                var cuerpo = $@"
                <html>
                <body style='background:#f5f5f5; padding:30px; font-family:Arial,sans-serif;'>
                    <div style='max-width:520px; margin:0 auto; background:white; padding:30px;
                                border-radius:14px; box-shadow:0 4px 12px rgba(0,0,0,0.15);'>

                        <div style='text-align:center; margin-bottom:20px;'>
                            <img src='{logoUrl}' alt='Neflis' style='height:50px; opacity:0.95;'/>
                        </div>

                        <h2 style='text-align:center; color:#222; margin-bottom:10px;'>
                            ¡Tu suscripción está activa!
                        </h2>

                        <p style='font-size:15px; color:#444;'>
                            Hola <strong>{usuario.NombreCompleto}</strong>,
                        </p>

                        <p style='font-size:15px; color:#444;'>
                            Gracias por suscribirte a <strong>Neflis</strong>.
                        </p>

                        <div style='background:#f8f8f8; padding:16px 18px; border-radius:10px; margin:18px 0;'>
                            <p><strong>Plan:</strong> {plan.NombrePlan}</p>
                            <p><strong>Precio:</strong> ₡{plan.Precio:N0}</p>
                            <p><strong>Perfiles máximos:</strong> {plan.MaxPerfiles}</p>
                        </div>

                        <p style='font-size:14px; color:#555;'>
                            Ya puedes crear tus perfiles y comenzar a disfrutar del catálogo.
                        </p>

                        <p style='font-size:12px; color:#999; text-align:center; margin-top:30px;'>
                            © 2025 - Neflis
                        </p>
                    </div>
                </body>
                </html>";

                await _emailService.EnviarCorreoAsync(usuario.Correo, asunto, cuerpo);
            }

            // ----------------------------------------------------------------

            var tienePerfiles = _context.Perfiles.Any(p => p.UsuarioId == usuarioId);

            if (!tienePerfiles)
                return RedirectToAction("Create", "Perfiles");

            return RedirectToAction("Index", "PerfilSelector");
        }

        // --------------------------------------------------------------------
        // CANCELAR
        // --------------------------------------------------------------------
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

            return RedirectToAction("Planes");
        }
    }
}
