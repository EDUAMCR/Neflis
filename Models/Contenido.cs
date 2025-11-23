namespace Neflis.Models
{
    public class Contenido
    {
        public int ContenidoId { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string? Sinopsis { get; set; }

        public int? Anio { get; set; }

        public string? Clasificacion { get; set; }   // +13, +18, etc.

        public string? Genero { get; set; }

        public string? UrlPortada { get; set; }

        public bool Disponible { get; set; } = true;

        public ICollection<CalificacionContenido> Calificaciones { get; set; } = new List<CalificacionContenido>();

    }
}
