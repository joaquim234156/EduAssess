namespace EduAssess.Application.DTOs.Nota
{
    public record NotaRequest(
        int IdAluno,
        int IdDisciplina,
        decimal ValorNota
        );
}
