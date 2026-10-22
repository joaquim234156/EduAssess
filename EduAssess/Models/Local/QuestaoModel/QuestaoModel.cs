namespace EduAssess.Models.Local
{
    public enum DificuldadeQuestao
    {
        Dificil = 2,
        Media = 1,
        Facil = 0,
    }
  
    public class QuestaoModel
    {
          protected QuestaoModel(){ }
        public QuestaoModel(Guid professorId, Guid disciplinaId, string enunciado, string alternativaCorreta,string alternativa1, string alternativa2, string alternativa3, string alternativa4, string alternativa5,string assunto,string explicacao, DificuldadeQuestao dificuldade = DificuldadeQuestao.Facil)
        {
            Id = Guid.NewGuid();
            ProfessorId = professorId;
            DisciplinaId = disciplinaId;
            Enunciado = enunciado;
            Alternativa1 = alternativa1;
            Alternativa2 = alternativa2;
            Alternativa3 = alternativa3;
            Alternativa4 = alternativa4;
            Alternativa5 = alternativa5;
            AlternativaCorreta = alternativaCorreta;
            Assunto = assunto;
            Explicacao = explicacao;
            Dificuldade = dificuldade;
            Ativa = true;
            
        }
        public Guid Id {get;init;}
        public Guid ProfessorId {get; private set;}
        public Guid DisciplinaId {get;private set;}
        public string Enunciado { get; private set; } = string.Empty;
        public string AlternativaCorreta{get;private set;} = string.Empty;
        public string Alternativa1 {get; private set;} = string.Empty;
        public string Alternativa2 {get; private set;} = string.Empty;
        public string Alternativa3 {get; private set;} = string.Empty;
        public string Alternativa4 {get; private set;} = string.Empty;
        public string Alternativa5 {get; private set;} = string.Empty;
        public string Assunto {get;private set; } = string.Empty;
        public string Explicacao { get; private set; } = string.Empty;
        public DificuldadeQuestao Dificuldade {get; private set;}
        public bool Ativa {get; private set;}

        public void UpdateQuestion(Guid disciplinaId, string enunciado, string alternativaCorreta,string alternativa1, string alternativa2, string alternativa3, string alternativa4, string alternativa5,string assunto,string explicacao,DificuldadeQuestao dificuldade )
        {
            DisciplinaId = disciplinaId;
            Enunciado = enunciado;
            Alternativa1 = alternativa1;
            Alternativa2 = alternativa2;
            Alternativa3 = alternativa3;
            Alternativa4 = alternativa4;
            Alternativa5 = alternativa5;
            AlternativaCorreta = alternativaCorreta;
            Assunto = assunto;
            Explicacao = explicacao;
            Dificuldade = dificuldade;
            Ativa = true;
        }
        public void Disable()
        {
            Ativa = false;
        }

        public void Enable()
        {
            Ativa = true;
        }
    }
}