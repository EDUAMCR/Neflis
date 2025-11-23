using Neflis.Models;
using System.ComponentModel.DataAnnotations;

namespace Neflis.Models
{
    public class MetodoPago
    {
        public int MetodoPagoId { get; set; }

        public int UsuarioId { get; set; }

        public string Tipo { get; set; } = "Tarjeta";

        // guardamos enmascarado
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
