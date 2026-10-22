using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Nota
{
    /// <param name="idMatricula">Matrícula do aluno que recebe a nota.</param>
    /// <param name="idUsuario">Professor (ou gestor) que está lançando a nota.</param>
    /// <param name="idDisciplina">Disciplina avaliada.</param>
    public record NotaRequest(
        Guid idMatricula,
        Guid idUsuario,
        Guid idDisciplina,
        [Required(ErrorMessage = "Informe a descrição da avaliação.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "A descrição deve ter entre 2 e 120 caracteres.")]
        string descricao,
        [Range(typeof(decimal), "0", "10", ErrorMessage = "A nota deve ser maior ou igual a 0 e menor ou igual a 10.")]
        decimal valorNota
        );
}
