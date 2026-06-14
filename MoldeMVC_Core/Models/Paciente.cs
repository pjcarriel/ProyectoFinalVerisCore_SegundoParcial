using System;
using System.Collections.Generic;

namespace MoldeMVC_Core.Models;

public partial class Paciente
{
    public int IdPaciente { get; set; }

    public string IdUsuario { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Cedula { get; set; } = null!;

    public int Edad { get; set; }

    public string Genero { get; set; } = null!;

    public int Estatura { get; set; }

    public double Peso { get; set; }

    public string Foto { get; set; } = null!;

    public virtual ICollection<Consulta> Consulta { get; set; } = new List<Consulta>();

    public virtual AspNetUser IdUsuarioNavigation { get; set; } = null!;
}
