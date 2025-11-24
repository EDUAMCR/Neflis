using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using Neflis.Models;
using System.Security.Claims;

namespace Neflis.Controllers
{
    [Authorize]
    public class MetodosPagoController : Controller
    {
        private readonly NeflisDbContext _context;

        public MetodosPagoController(NeflisDbContext context)
        {
            _context = context;
        }

        private int GetUsuarioId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // LISTA
        public IActionResult Index()
        {
            var usuarioId = GetUsuarioId();
            var metodos = _context.MetodosPago
                .Where(m => m.UsuarioId == usuarioId)
                .ToList();

            return View(metodos);
        }

        // CREAR
        public IActionResult Create(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new MetodoPago());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MetodoPago model, string? returnUrl = null)
        {
            var usuarioId = GetUsuarioId();
            model.UsuarioId = usuarioId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // enmascarar número usando los últimos 4 dígitos
            model.NumeroEnmascarado = $"****{model.NumeroEnmascarado}";

            // si este es predeterminado, desmarcar los otros
            if (model.EsPredeterminado)
            {
                var otros = _context.MetodosPago
                    .Where(m => m.UsuarioId == usuarioId && m.EsPredeterminado)
                    .ToList();

                foreach (var m in otros)
                {
                    m.EsPredeterminado = false;
                }
            }

            _context.MetodosPago.Add(model);
            _context.SaveChanges();

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("MiPlan", "Suscripciones");
        }

        // MARCAR COMO PREDETERMINADO
        public IActionResult Predeterminar(int id)
        {
            var usuarioId = GetUsuarioId();
            var metodos = _context.MetodosPago
                .Where(m => m.UsuarioId == usuarioId)
                .ToList();

            foreach (var m in metodos)
                m.EsPredeterminado = (m.MetodoPagoId == id);

            _context.SaveChanges();

            TempData["Mensaje"] = "Método de pago predeterminado actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ELIMINAR
        public IActionResult Eliminar(int id)
        {
            var metodo = _context.MetodosPago.Find(id);
            if (metodo != null)
            {
                _context.MetodosPago.Remove(metodo);
                _context.SaveChanges();
                TempData["Mensaje"] = "Método eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
