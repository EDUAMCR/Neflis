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

        // =========================================================
        //  REGISTRO
        // =========================================================

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
                // ⚠️ Si luego quieres hash, aquí es donde se aplica
                Password = model.Password,
                Rol = "Suscriptor",
                EstaActivo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            TempData["Mensaje"] = "Usuario registrado correctamente. Inicie sesión.";
            return RedirectToAction("Login");
        }

        // =========================================================
        //  LOGIN / LOGOUT
        // =========================================================

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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.Recordarme
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // GET/POST: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================================
        //  RECUPERAR CONTRASEÑA (REAL, CON CORREO)
        // =========================================================

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                ModelState.AddModelError("", "Debes ingresar un correo.");
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == correo);

            // Por seguridad: aunque no exista, se muestra el mismo mensaje
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
                    new { token = token, correo = usuario.Correo },
                    protocol: HttpContext.Request.Scheme
                );

                var asunto = "Neflis - Restablecer contraseña";
                var cuerpo = $@"
<p>Hola {usuario.NombreCompleto},</p>
<p>Hemos recibido una solicitud para restablecer tu contraseña en <strong>Neflis</strong>.</p>
<p>Haz clic en el siguiente enlace (válido por 1 hora):</p>
<p><a href=""{link}"">Restablecer contraseña</a></p>
<p>Si no fuiste tú, puedes ignorar este correo.</p>";

                await _emailService.EnviarCorreoAsync(usuario.Correo, asunto, cuerpo);
            }

            ViewBag.Mensaje = "Si el correo existe en el sistema, se enviaron instrucciones para restablecer la contraseña.";
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string correo)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(correo))
                return BadRequest("Datos inválidos.");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == correo && u.ResetPasswordToken == token);

            if (usuario == null ||
                usuario.ResetPasswordTokenExpira == null ||
                usuario.ResetPasswordTokenExpira < DateTime.UtcNow)
            {
                return View("ResetPasswordInvalido");
            }

            var vm = new ResetPasswordViewModel
            {
                Correo = correo,
                Token = token
            };

            return View(vm);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NuevaContrasena != model.ConfirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View(model);
            }

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

            // ⚠️ AJUSTA ESTA LÍNEA AL CAMPO REAL DE TU MODELO
            // Aquí usas la MISMA lógica que en Register (hash si luego lo agregas)
            usuario.Password = model.NuevaContrasena;

            usuario.ResetPasswordToken = null;
            usuario.ResetPasswordTokenExpira = null;

            await _context.SaveChangesAsync();

            ViewBag.Mensaje = "Tu contraseña se ha restablecido correctamente. Ahora puedes iniciar sesión.";
            return View("ResetPasswordConfirmado");
        }

        // =========================================================
        //  PRUEBA DE ENVÍO DE CORREO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.EnviarCorreoAsync(
                "prynelfis@gmail.com",
                "Prueba Neflis",
                "<h1>¡Funciona el correo!</h1><p>Esto es una prueba desde Neflis.</p>"
            );

            return Content("Correo enviado correctamente ✔");
        }
    }
}
