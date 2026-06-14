using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Especialidade
{
    public int IdEspecialidad { get; set; }

    public string Descripcion { get; set; } = null!;

    public string Dias { get; set; } = null!;

    public TimeOnly FranjaHi { get; set; }

    public TimeOnly FranjaHf { get; set; }

    public virtual ICollection<Medico> Medicos { get; set; } = new List<Medico>();
}
