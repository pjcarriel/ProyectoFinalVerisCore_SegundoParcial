using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Receta
{
    public int IdReceta { get; set; }

    public int IdConsulta { get; set; }

    public int IdMedicamento { get; set; }

    public int Cantidad { get; set; }

    public virtual Consulta IdConsultaNavigation { get; set; } = null!;

    public virtual Medicamento IdMedicamentoNavigation { get; set; } = null!;
}
