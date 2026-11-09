using EduAssess.Application.DTOs.Questao;
using EduAssess.Models;
using EduAssess.Models.Local;

namespace EduAssess.Application.Interfaces
{
    public interface IIaQuestaoService
    {
        // ALTERADO: ganhou o parâmetro opcional "jaExistentes". Os enunciados que o professor
        // já tem no banco são enviados à IA com a instrução "não repita estas", evitando
        // questões duplicadas quando ele gera várias vezes o mesmo assunto.
        Task<List<QuestaoRequest>> GerarQuestoesAsync(
            Guid disciplinaId,
            string nomeDisciplina,
            string assunto,
            DificuldadeQuestao dificuldade,
            int quantidade,
            IReadOnlyCollection<string>? jaExistentes = null,
            // NOVO: orientações livres de quem pediu as questões; entram no prompt da IA.
            string? recomendacoes = null);
    }
}
