using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Neflis.Data;
using Neflis.Models;
using Neflis.Models.ViewModels;

namespace Neflis.Controllers
{
    public class AccountController : Controller
    {
        private readonly NeflisDbContext _context;

        public AccountController(NeflisDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegistroViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // validar correo único
            var existe = _context.Usuarios.FirstOrDefault(u => u.Correo == model.Correo);
            if (existe != null)
            {
                ModelState.AddModelError(string.Empty, "Ya existe un usuario con ese correo.");
                return View(model);
            }

            var usuario = new Usuario
            {
                Correo = model.Correo,
                NombreCompleto = model.NombreCompleto,
                Password = model.Password, // luego lo encriptamos
                Rol = "Suscriptor",
                EstaActivo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            // después de registrar, lo mandamos a login
            TempData["Mensaje"] = "Usuario registrado correctamente. Inicie sesión.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == model.Correo);

            if (usuario == null || usuario.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

            if (!usuario.EstaActivo)
            {
                ModelState.AddModelError(string.Empty, "El usuario está bloqueado.");
                return View(model);
            }

            // crear identidad
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.Recordarme
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/RecuperarPassword
        public IActionResult RecuperarPassword()
        {
            return View();
        }

        // POST: /Account/RecuperarPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecuperarPassword(RecuperarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == model.Correo);
            if (usuario == null)
            {
                // no decimos que no existe para no dar pista
                TempData["Mensaje"] = "Si el correo existe, se envió un enlace de recuperación.";
                return RedirectToAction("RecuperarPassword");
            }

            // SIMULACIÓN: generamos un token y lo mostramos
            var token = Guid.NewGuid().ToString("N");

            TempData["Mensaje"] = $"Simulación: usa este token para restablecer: {token}";
            TempData["Token"] = token;
            TempData["Correo"] = model.Correo;

            return RedirectToAction("RestablecerPassword", new { token = token, correo = model.Correo });
        }

        // GET: /Account/RestablecerPassword
        public IActionResult RestablecerPassword(string token, string correo)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(correo))
                return RedirectToAction("Login");

            var model = new RestablecerPasswordViewModel
            {
                Token = token
            };

            ViewBag.Correo = correo;
            return View(model);
        }

        // POST: /Account/RestablecerPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestablecerPassword(string correo, RestablecerPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Correo = correo;
                return View(model);
            }

            // en un escenario real validaríamos el token contra BD
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);
            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
                return View(model);
            }

            usuario.Password = model.NuevoPassword;
            _context.SaveChanges();

            TempData["Mensaje"] = "Contraseña actualizada. Inicie sesión.";
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
