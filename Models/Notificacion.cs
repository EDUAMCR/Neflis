using System;

namespace Neflis.Models
{
    public class Notificacion
    {
        public int NotificacionId { get; set; }

        public int UsuarioId { get; set; }

        public string Asunto { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public bool Leida { get; set; } = false;

        public Usuario Usuario { get; set; }
    }
}
