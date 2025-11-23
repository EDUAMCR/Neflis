using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using Neflis.Models;

namespace Neflis.Controllers
{
    [Authorize]
    public class MiListaController : Controller
    {
        private readonly NeflisDbContext _context;

        public MiListaController(NeflisDbContext context)
        {
            _context = context;
        }

        private int? GetPerfilActualId()
        {
            return HttpContext.Session.GetInt32("PerfilIdActual");
        }

        public IActionResult Index()
        {
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
                return RedirectToAction("Index", "PerfilSelector");

            var lista = _context.MiLista
                .Where(m => m.PerfilId == perfilId)
                .Select(m => m.Contenido)
                .ToList();

            return View(lista);
        }

        public IActionResult Agregar(int id)
        {
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
                return RedirectToAction("Index", "PerfilSelector");

            // si ya está, no duplica
            var existe = _context.MiLista.FirstOrDefault(m => m.PerfilId == perfilId && m.ContenidoId == id);
            if (existe == null)
            {
                _context.MiLista.Add(new MiLista
                {
                    PerfilId = perfilId.Value,
                    ContenidoId = id
                });
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Catalogo");
        }

        public IActionResult Quitar(int id)
        {
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
                return RedirectToAction("Index", "PerfilSelector");

            var item = _context.MiLista.FirstOrDefault(m => m.PerfilId == perfilId && m.ContenidoId == id);
            if (item != null)
            {
                _context.MiLista.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
