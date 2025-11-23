using System;

namespace Neflis.Models
{
    public class MiLista
    {
        public int MiListaId { get; set; }

        public int PerfilId { get; set; }

        public int ContenidoId { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;

        // navegación
        public Perfil Perfil { get; set; }
        public Contenido Contenido { get; set; }
    }
}
