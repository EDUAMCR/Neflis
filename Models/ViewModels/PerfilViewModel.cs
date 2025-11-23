using System.ComponentModel.DataAnnotations;

namespace Neflis.Models.ViewModels
{
    public class PerfilViewModel
    {
        public int? PerfilId { get; set; }

        [Required]
        [StringLength(50)]
        public string NombrePerfil { get; set; }

        public bool EsInfantil { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
