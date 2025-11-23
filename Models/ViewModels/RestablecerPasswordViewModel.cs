using System.ComponentModel.DataAnnotations;

namespace Neflis.Models.ViewModels
{
    public class RestablecerPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NuevoPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NuevoPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }
    }
}
