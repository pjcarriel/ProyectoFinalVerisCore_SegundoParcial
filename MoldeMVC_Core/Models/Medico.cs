using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Medico
{
    public int IdMedico { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdEspecialidad { get; set; }

    public string IdUsuario { get; set; } = null!;

    public string Foto { get; set; } = null!;

    public virtual ICollection<Consulta> Consulta { get; set; } = new List<Consulta>();

    public virtual Especialidade IdEspecialidadNavigation { get; set; } = null!;

    public virtual AspNetUser IdUsuarioNavigation { get; set; } = null!;
}
