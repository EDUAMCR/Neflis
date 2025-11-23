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
        public IActionResult Create(MetodoPago model, string? returnUrl)
        {
            var usuarioId = GetUsuarioId();

            if (!string.IsNullOrWhiteSpace(model.NumeroEnmascarado) &&
                model.NumeroEnmascarado.Length >= 4)
            {
                model.NumeroEnmascarado = "**** **** **** " +
                    model.NumeroEnmascarado[^4..];
            }

            model.UsuarioId = usuarioId;

            if (model.EsPredeterminado)
            {
                var otros = _context.MetodosPago
                    .Where(m => m.UsuarioId == usuarioId);
                foreach (var o in otros)
                    o.EsPredeterminado = false;
            }

            _context.MetodosPago.Add(model);
            _context.SaveChanges();

            TempData["Mensaje"] = "Método de pago agregado correctamente.";

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
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
