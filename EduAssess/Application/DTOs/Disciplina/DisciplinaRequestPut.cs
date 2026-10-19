namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaRequestPut(
        string nome,
        string descricao,
        Guid professorId
        );
}