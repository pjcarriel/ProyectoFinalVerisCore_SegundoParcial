using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Medicamento
{
    public int IdMedicamento { get; set; }

    public string Nombre { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public virtual ICollection<Receta> Receta { get; set; } = new List<Receta>();
}
