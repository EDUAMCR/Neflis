using System;
using System.Collections.Generic;

namespace Neflis.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        // correo para login
        public string Correo { get; set; } = string.Empty;

        // hash o password plano por ahora (luego lo cambiamos)
        public string Password { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        // Admin, Suscriptor, Soporte
        public string Rol { get; set; } = "Suscriptor";

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Activo / Bloqueado
        public bool EstaActivo { get; set; } = true;

        // navegación
        public ICollection<Perfil> Perfiles { get; set; }
        public ICollection<SuscripcionUsuario> Suscripciones { get; set; }
        public ICollection<MetodoPago> MetodosPago { get; set; }
    }
}
