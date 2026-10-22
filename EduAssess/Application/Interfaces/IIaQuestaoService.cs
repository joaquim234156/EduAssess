using EduAssess.Application.DTOs.Questao;
using EduAssess.Models;
using EduAssess.Models.Local;

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