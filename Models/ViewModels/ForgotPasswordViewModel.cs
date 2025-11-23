using System.ComponentModel.DataAnnotations;

namespace Neflis.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; }
    }
}
