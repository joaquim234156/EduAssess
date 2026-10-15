namespace EduAssess.Application.DTOs.Nota
{
    public record NotaRequest(
        Guid IdUsuario,
        Guid IdDisciplina,
        Guid idTurma,
        Guid idCurso,
        decimal ValorNota
        );
}
