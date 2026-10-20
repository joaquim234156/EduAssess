namespace EduAssess.Application.DTOs.Disciplina
{
    // O Professor usa o ID do token; Admin/Gestor informam professorId ao criar a disciplina.
    public record DisciplinaRequest(
        string nome,
        string descricao,
        Guid? professorId = null
        );
}