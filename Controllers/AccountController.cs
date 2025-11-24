using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neflis.Data;
using Neflis.Models;
using Neflis.Models.ViewModels;
using Neflis.Services;
using System.Security.Claims;

namespace Neflis.Controllers
{
    public class AccountController : Controller
    {
        private readonly NeflisDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(NeflisDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // --------------------------------------------------------------------
        // REGISTRO
        // --------------------------------------------------------------------
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
                Password = model.Password, // TODO: encriptar en el futuro
                Rol = "Suscriptor",
                EstaActivo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            TempData["Mensaje"] = "Usuario registrado correctamente. Inicie sesión.";
            return RedirectToAction("Login");
        }

        // --------------------------------------------------------------------
        // LOGIN / LOGOUT
        // --------------------------------------------------------------------
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

        // --------------------------------------------------------------------
        // FLUJO 1 (SIMULADO): RecuperarPassword / RestablecerPassword
        // --------------------------------------------------------------------
        public IActionResult RecuperarPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecuperarPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == model.Correo);
            if (usuario == null)
            {
                TempData["Mensaje"] = "Si el correo existe, se envió un enlace de recuperación.";
                return RedirectToAction("RecuperarPassword");
            }

            var token = Guid.NewGuid().ToString("N");

            TempData["Mensaje"] = $"Simulación: usa este token para restablecer: {token}";
            TempData["Token"] = token;
            TempData["Correo"] = model.Correo;

            return RedirectToAction("RestablecerPassword", new { token, correo = model.Correo });
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestablecerPassword(string correo, RestablecerPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Correo = correo;
                return View(model);
            }

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

        // --------------------------------------------------------------------
        // FLUJO 2 (REAL CON CORREO): ForgotPassword / ResetPassword
        // --------------------------------------------------------------------
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // le pasamos un VM vacío a la vista
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == model.Correo);

            // Por seguridad: aunque no exista, no decimos que no existe
            if (usuario != null)
            {
                // Generar token
                var token = Guid.NewGuid().ToString("N");
                usuario.ResetPasswordToken = token;
                usuario.ResetPasswordTokenExpira = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                // Link absoluto a ResetPassword
                var link = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = token, email = usuario.Correo },
                    protocol: HttpContext.Request.Scheme);

                var asunto = "Neflis - Restablecer contraseña";
                var cuerpo = $@"
            <p>Hola {usuario.NombreCompleto},</p>
            <p>Hemos recibido una solicitud para restablecer tu contraseña en <strong>Neflis</strong>.</p>
            <p>Haz clic en el siguiente enlace para continuar:</p>
            <p><a href=""{link}"">Restablecer contraseña</a></p>
            <p>Si no fuiste tú, puedes ignorar este correo.</p>";

                await _emailService.EnviarCorreoAsync(usuario.Correo, asunto, cuerpo);
            }

            ViewBag.Mensaje = "Si el correo existe en el sistema, se enviaron instrucciones para restablecer la contraseña.";
            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest("Datos inválidos.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == email && u.ResetPasswordToken == token);

            if (usuario == null ||
                usuario.ResetPasswordTokenExpira == null ||
                usuario.ResetPasswordTokenExpira < DateTime.UtcNow)
            {
                return View("ResetPasswordInvalido");
            }

            var vm = new ResetPasswordViewModel
            {
                Correo = email,
                Token = token
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo == model.Correo &&
                    u.ResetPasswordToken == model.Token);

            if (usuario == null ||
                usuario.ResetPasswordTokenExpira == null ||
                usuario.ResetPasswordTokenExpira < DateTime.UtcNow)
            {
                return View("ResetPasswordInvalido");
            }

            usuario.Password = model.NuevaContrasena;
            usuario.ResetPasswordToken = null;
            usuario.ResetPasswordTokenExpira = null;

            await _context.SaveChangesAsync();

            ViewBag.Mensaje = "Tu contraseña se ha restablecido correctamente. Ahora puedes iniciar sesión.";
            return View("ResetPasswordConfirmado");
        }

        // --------------------------------------------------------------------
        // PRUEBA DE ENVÍO DE CORREO
        // --------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> TestEmail([FromServices] IEmailService email)
        {
            await email.EnviarCorreoAsync(
                "prynelfis@gmail.com",
                "Prueba Neflis",
                "<h1>¡Funciona el correo!</h1><p>Esto es una prueba desde Neflis.</p>"
            );

            return Content("Correo enviado correctamente ✔");
        }


    }
}
