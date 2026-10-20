using EduAssess.Models;

namespace EduAssess.Application.DTOs.Questao
{
    public record QuestaoResponseDTO(
        Guid Id,
        Guid DisciplinaId,
        Guid ProfessorId,
        string Enunciado,
        string Alternativa1,
        string Alternativa2,
        string Alternativa3,
        string Alternativa4,
        string Alternativa5,
        string AlternativaCorreta,
        string Assunto,
        string Explicacao,
        DificuldadeQuestao Dificuldade,
        bool Ativa
    );
}