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

        [Required]
        [Display(Name = "Últimos 4 dígitos")]
        [RegularExpression(@"^\d{4}$",
            ErrorMessage = "Los últimos 4 dígitos deben ser solo números.")]
        public string NumeroEnmascarado { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha de vencimiento (MM/AA)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$",
            ErrorMessage = "Usa el formato MM/AA, por ejemplo 05/27.")]
        public string FechaVencimiento { get; set; } = string.Empty;
        public string? Vence { get; set; }

        public bool EsPredeterminado { get; set; } = false;

        public Usuario Usuario { get; set; }
    }
}
