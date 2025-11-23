using Neflis.Models;

namespace Neflis.Models
{
    public class Perfil
    {
        public int PerfilId { get; set; }

        public int UsuarioId { get; set; }

        public string NombrePerfil { get; set; } = string.Empty;

        // Adulto / Niño o lo que definamos
        public bool EsInfantil { get; set; } = false;

        public string? AvatarUrl { get; set; }

        // nav
        public Usuario Usuario { get; set; }
    }
}
