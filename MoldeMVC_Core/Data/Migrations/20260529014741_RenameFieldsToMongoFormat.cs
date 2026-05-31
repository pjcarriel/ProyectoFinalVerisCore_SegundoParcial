using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoldeMVC_Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameFieldsToMongoFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "idMedicamento",
                table: "Recetas",
                newName: "medicamentoId");

            migrationBuilder.RenameColumn(
                name: "idConsulta",
                table: "Recetas",
                newName: "consultaId");

            migrationBuilder.RenameColumn(
                name: "idEspecialidad",
                table: "Medicos",
                newName: "especialidadId");

            migrationBuilder.RenameColumn(
                name: "idPaciente",
                table: "Consultas",
                newName: "pacienteId");

            migrationBuilder.RenameColumn(
                name: "idMedico",
                table: "Consultas",
                newName: "medicoId");

            migrationBuilder.RenameColumn(
                name: "idEspecialidad",
                table: "Consultas",
                newName: "especialidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "medicamentoId",
                table: "Recetas",
                newName: "idMedicamento");

            migrationBuilder.RenameColumn(
                name: "consultaId",
                table: "Recetas",
                newName: "idConsulta");

            migrationBuilder.RenameColumn(
                name: "especialidadId",
                table: "Medicos",
                newName: "idEspecialidad");

            migrationBuilder.RenameColumn(
                name: "pacienteId",
                table: "Consultas",
                newName: "idPaciente");

            migrationBuilder.RenameColumn(
                name: "medicoId",
                table: "Consultas",
                newName: "idMedico");

            migrationBuilder.RenameColumn(
                name: "especialidadId",
                table: "Consultas",
                newName: "idEspecialidad");
        }
    }
}
