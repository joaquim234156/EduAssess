namespace EduAssess.Application.DTOs.Matricula
{
    public record MatriculaRequest(
        Guid usuarioId,
        Guid turmaId
        );
}
