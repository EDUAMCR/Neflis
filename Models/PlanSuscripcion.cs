using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neflis.Models
{
    public class PlanSuscripcion
    {
        public int PlanSuscripcionId { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public int PeriodoMeses { get; set; } = 1;

        [Precision(18, 2)] //
        public decimal Precio { get; set; }

        public int MaxPerfiles { get; set; } = 4;
    }
}
