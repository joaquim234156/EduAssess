namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaRequest(
        string nome,
        string descricao,
        Guid? professorId = null
        );
}