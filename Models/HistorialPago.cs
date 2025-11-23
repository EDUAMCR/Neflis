using System;

namespace Neflis.Models
{
    public class HistorialPago
    {
        public int HistorialPagoId { get; set; }

        public int UsuarioId { get; set; }

        public int? SuscripcionUsuarioId { get; set; }

        public decimal Monto { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        // Aprobado, Rechazado, Simulado
        public string Estado { get; set; } = "Aprobado";

        // opcional: método usado
        public string? Metodo { get; set; }

        // navegación
        public Usuario Usuario { get; set; }
        public SuscripcionUsuario? SuscripcionUsuario { get; set; }
    }
}
