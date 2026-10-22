using Microsoft.AspNetCore.Components;

namespace EduAssess.Models
{
    public enum QuestaoRole
    {
        Dificil = 2,
        Media = 1,
        Facil = 0,
    }
    public class QuestaoModel
    {
        public QuestaoModel(Guid professorId, Guid disciplinaId, string enunciado, string alternativaCorreta,string assunto,string explicacao)
        {
            Id = Guid.NewGuid();
            ProfessorId = professorId;
            Disciplina = disciplinaId;
            Enunciado = enunciado;
            AlternativaCorreta = alternativaCorreta;
            Assunto = assunto;
            Explicacao = explicacao;
            Ativa = true;
            
        }
        public Guid Id {get;init;}
        public Guid ProfessorId {get; private set;}
        public Guid Disciplina {get;private set;}
        public string Enunciado {get;private set;}
        public string AlternativaCorreta{get;private set;}
        public string Assunto {get;private set;}
        public string Explicacao {get; private set;}
        public bool Ativa {get; private set;}

        public void CreateQuestion(Guid professorId, Guid disciplinaId, string enunciado, string alternativaCorreta, string assunto, string explicacao)
        {
            ProfessorId = professorId;
            Disciplina = disciplinaId;
            Enunciado = enunciado;
            AlternativaCorreta = alternativaCorreta;
            Assunto = assunto;
            Explicacao = explicacao;
        }
        public void Disable()
        {
            Ativa = false;
        }
    }
}