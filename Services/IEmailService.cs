using Microsoft.AspNetCore.Mvc;

namespace Neflis.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string destino, string asunto, string cuerpoHtml);
    }
}
