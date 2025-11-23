using System;

namespace Neflis.Models
{
    public class CalificacionContenido
    {
        public int CalificacionContenidoId { get; set; }

        public int ContenidoId { get; set; }

        public int PerfilId { get; set; }
        public int UsuarioId { get; set; }

        public int Estrellas { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Contenido Contenido { get; set; }

        public Perfil Perfil { get; set; }

        public Usuario Usuario { get; set; }
    }
}
