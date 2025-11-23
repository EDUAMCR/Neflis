using Neflis.Models;
using System.ComponentModel.DataAnnotations;

namespace Neflis.Models
{
    public class MetodoPago
    {
        public int MetodoPagoId { get; set; }

        public int UsuarioId { get; set; }

        [Required]
        public string Tipo { get; set; } = "Tarjeta";

        // guardamos enmascarado
        [Required]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Ingrese exactamente 4 dígitos.")]
        public string NumeroEnmascarado { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria")]
        [StringLength(5, ErrorMessage = "Formato inválido (MM/AA)")]
        public string FechaVencimiento { get; set; }   
        public string? Vence { get; set; }

        public bool EsPredeterminado { get; set; } = false;

        // nav
        public Usuario Usuario { get; set; }
    }
}
