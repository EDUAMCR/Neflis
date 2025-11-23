using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neflis.Data;
using Neflis.Models;

namespace Neflis.Controllers
{
    [Authorize]
    public class CatalogoController : Controller
    {
        private readonly NeflisDbContext _context;

        public CatalogoController(NeflisDbContext context)
        {
            _context = context;
        }

        private int? GetPerfilActualId() => HttpContext.Session.GetInt32("PerfilIdActual");

        private bool PerfilEsInfantil()
        {
            var v = HttpContext.Session.GetString("PerfilEsInfantil");
            return v == "1";
        }

        private bool ContenidoDesbloqueado()
        {
            return HttpContext.Session.GetString("ContenidoDesbloqueado") == "1";
        }

        // GET: /Catalogo
        public IActionResult Index(string? genero, string? buscar)
        {
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
                return RedirectToAction("Index", "PerfilSelector");

            var query = _context.Contenidos.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(c => c.Titulo.Contains(buscar));

            if (!string.IsNullOrEmpty(genero))
                query = query.Where(c => c.Genero == genero);

            // filtro infantil o si no ha desbloqueado contenido restringido
            if (PerfilEsInfantil() || !ContenidoDesbloqueado())
            {
                // ocultamos cualquier contenido +18
                query = query.Where(c => c.Clasificacion == null || c.Clasificacion != "+18");
            }

            var lista = query
                .Where(c => c.Disponible)
                .OrderBy(c => c.Titulo)
                .ToList();

            var generos = _context.Contenidos
                .Where(c => c.Genero != null)
                .Select(c => c.Genero)
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            ViewBag.Generos = generos;
            ViewBag.GeneroActual = genero;
            ViewBag.Buscar = buscar;

            return View(lista);
        }

        // GET: /Catalogo/Detalle/5
        // GET: /Catalogo/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            // 1) Cargar el contenido
            var contenido = await _context.Contenidos
                .FirstOrDefaultAsync(c => c.ContenidoId == id);

            if (contenido == null)
                return NotFound();

            // 2) Comentarios del contenido
            var comentarios = await _context.ComentariosContenido
                .Where(c => c.ContenidoId == id)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            ViewBag.Comentarios = comentarios;

            // 3) Query base de calificaciones (la reutilizamos)
            var califsQuery = _context.CalificacionesContenido
                .Where(c => c.ContenidoId == id);

            // Total de calificaciones
            var total = await califsQuery.CountAsync();

            // Promedio de estrellas (si hay calificaciones)
            double promedio = 0;
            if (total > 0)
            {
                promedio = await califsQuery.AverageAsync(c => c.Estrellas);
            }

            ViewBag.Promedio = promedio;
            ViewBag.TotalCalificaciones = total;

            // 4) Tu calificación (del PERFIL actual)
            int miCalif = 0;
            var perfilId = GetPerfilActualId();

            if (perfilId != null)
            {
                int pid = perfilId.Value;

                var tuCalif = await califsQuery
                    .FirstOrDefaultAsync(c => c.PerfilId == pid);

                miCalif = tuCalif?.Estrellas ?? 0;
            }

            ViewBag.MiCalificacion = miCalif;

            // 5) Devolvemos la vista de detalle
            return View(contenido);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AgregarComentario(int contenidoId, string texto)
        {
            var usuarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (usuarioIdClaim == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(texto))
            {
                TempData["MensajeError"] = "El comentario no puede estar vacío.";
                return RedirectToAction("Detalle", new { id = contenidoId });
            }

            var comentario = new ComentarioContenido
            {
                ContenidoId = contenidoId,
                UsuarioId = int.Parse(usuarioIdClaim.Value),
                Texto = texto.Trim(),
                Fecha = DateTime.UtcNow
            };

            _context.ComentariosContenido.Add(comentario);
            _context.SaveChanges();

            TempData["Mensaje"] = "Comentario agregado.";
            return RedirectToAction("Detalle", new { id = contenidoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Calificar(int contenidoId, int estrellas)
        {
            if (estrellas < 1 || estrellas > 5)
            {
                TempData["MensajeError"] = "La calificación debe ser entre 1 y 5.";
                return RedirectToAction("Detalle", new { id = contenidoId });
            }

            // PERFIL actual
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
            {
                TempData["MensajeError"] = "No se encontró el perfil actual.";
                return RedirectToAction("Detalle", new { id = contenidoId });
            }
            int pid = perfilId.Value;

            // USUARIO actual (para llenar UsuarioId)
            var usuarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (usuarioIdClaim == null)
            {
                TempData["MensajeError"] = "No se encontró el usuario actual.";
                return RedirectToAction("Detalle", new { id = contenidoId });
            }
            int usuarioId = int.Parse(usuarioIdClaim.Value);

            var califExistente = _context.CalificacionesContenido
                .FirstOrDefault(c => c.ContenidoId == contenidoId
                                  && c.PerfilId == pid);

            if (califExistente == null)
            {
                _context.CalificacionesContenido.Add(new CalificacionContenido
                {
                    ContenidoId = contenidoId,
                    PerfilId = pid,
                    UsuarioId = usuarioId,      // 👈 importante
                    Estrellas = estrellas,
                    Fecha = DateTime.UtcNow
                });
            }
            else
            {
                califExistente.Estrellas = estrellas;
                califExistente.Fecha = DateTime.UtcNow;
                califExistente.UsuarioId = usuarioId; // por si acaso
            }

            _context.SaveChanges();
            TempData["Mensaje"] = "Calificación guardada.";
            return RedirectToAction("Detalle", new { id = contenidoId });
        }

        // GET: /Catalogo/Aleatorio
        public IActionResult Aleatorio()
        {
            var perfilId = GetPerfilActualId();
            if (perfilId == null)
                return RedirectToAction("Index", "PerfilSelector");

            var query = _context.Contenidos
                .Where(c => c.Disponible);

            if (PerfilEsInfantil() && !ContenidoDesbloqueado())
                query = query.Where(c => c.Clasificacion == null || c.Clasificacion != "+18");

            var randomContenido = query
                .OrderBy(c => Guid.NewGuid())
                .FirstOrDefault();

            if (randomContenido == null)
            {
                TempData["MensajeError"] = "No hay contenido disponible para mostrar.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Detalle", new { id = randomContenido.ContenidoId });
        }
    }
}
