using EduAssess.Application.DTOs.Questao;
using EduAssess.Models;

namespace EduAssess.Application.Interfaces
{
    public interface IIaQuestaoService
    {
        Task<List<QuestaoRequest>> GerarQuestoesAsync(
            Guid disciplinaId,
            string nomeDisciplina,
            string assunto,
            DificuldadeQuestao dificuldade,
            int quantidade);
    }
}