namespace EduAssess.Application.DTOs.Matricula
{
    public record MatriculaRequestPut(
        Guid usuarioId,
        Guid turmaId
        );
}