using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Consulta
{
    public int IdConsulta { get; set; }

    public int IdMedico { get; set; }

    public int IdPaciente { get; set; }

    public DateOnly FechaConsulta { get; set; }

    public TimeOnly Hi { get; set; }

    public TimeOnly Hf { get; set; }

    public string Diagnostico { get; set; } = null!;

    public virtual Medico IdMedicoNavigation { get; set; } = null!;

    public virtual Paciente IdPacienteNavigation { get; set; } = null!;

    public virtual ICollection<Receta> Receta { get; set; } = new List<Receta>();
}
