using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Nota
{
    // O professor não vem no JSON; a rota obtém seu ID da identidade autenticada.
    public record NotaRequest(
        [Required(ErrorMessage = "Selecione um aluno.")]
        Guid idMatricula,
        [Required(ErrorMessage = "Selecione uma disciplina.")]
        Guid idDisciplina,
        string descricao,
        [Required(ErrorMessage = "A nota é obrigatória.")]
        [Range(typeof(decimal), "0", "10", ErrorMessage = "A nota deve ser maior ou igual a 0 e menor ou igual a 10.")]
        decimal valorNota
        );
}
