using System.ComponentModel.DataAnnotations;

namespace Neflis.Models.ViewModels
{
    public class RecuperarPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; }
    }
}
