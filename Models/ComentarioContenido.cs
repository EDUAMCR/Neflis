using System;

namespace Neflis.Models
{
    public class ComentarioContenido
    {
        public int ComentarioContenidoId { get; set; }

        public int ContenidoId { get; set; }

        public int UsuarioId { get; set; }

        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // navegación
        public Contenido Contenido { get; set; }
        public Usuario Usuario { get; set; }
    }
}
