using Neflis.Models;
using System;

namespace Neflis.Models
{
    public class SuscripcionUsuario
    {
        public int SuscripcionUsuarioId { get; set; }

        public int UsuarioId { get; set; }

        public int PlanSuscripcionId { get; set; }

        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;

        public DateTime? FechaFin { get; set; }

        // Activa, Cancelada, Vencida
        public string Estado { get; set; } = "Activa";

        // nav
        public Usuario Usuario { get; set; }
        public PlanSuscripcion PlanSuscripcion { get; set; }
    }
}
