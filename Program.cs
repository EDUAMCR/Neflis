using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Neflis.Data;
using Neflis.Models;
using Neflis.Services;

namespace Neflis
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. SERVICES
            builder.Services.AddControllersWithViews();

            // ---------------------------------------------
            // DbContext: SQL Server en Development, SQLite en Producción (Render)
            // ---------------------------------------------
            var connectionString = builder.Environment.IsDevelopment()
                ? builder.Configuration.GetConnectionString("DefaultConnection")   // SQL Server local
                : builder.Configuration.GetConnectionString("RenderConnection");   // SQLite en Render

            builder.Services.AddDbContext<NeflisDbContext>(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.UseSqlServer(connectionString);
                }
                else
                {
                    options.UseSqlite(connectionString);
                }
            });

            // Autenticación por cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

            // Email settings
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            // Servicio de correo
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Necesario para leer la sesión desde _Layout:
            builder.Services.AddHttpContextAccessor();

            // Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // 2. PIPELINE
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // usar sesión
            app.UseSession();

            // Primero auth, luego authorization.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // 3. Inicializar BD (SQLite en Render o SQL local) + SEED de planes
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NeflisDbContext>();

                // Crea la BD si no existe (para SQLite en Render y para SQL en dev si hace falta)
                db.Database.EnsureCreated();

                if (!db.PlanesSuscripcion.Any())
                {
                    db.PlanesSuscripcion.AddRange(
                        new PlanSuscripcion { NombrePlan = "Básico", PeriodoMeses = 1, Precio = 3500, MaxPerfiles = 2 },
                        new PlanSuscripcion { NombrePlan = "Estándar", PeriodoMeses = 1, Precio = 5500, MaxPerfiles = 4 },
                        new PlanSuscripcion { NombrePlan = "Premium", PeriodoMeses = 1, Precio = 7500, MaxPerfiles = 5 }
                    );
                    db.SaveChanges();
                }
            }

            app.Run();
        }
    }
}
