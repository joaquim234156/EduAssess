using System.ComponentModel.DataAnnotations;
using EduAssess.Models.Local;

namespace EduAssess.Application.DTOs.Questao
{
    public record QuestaoRequest(
        [Required(ErrorMessage = "A disciplina é obrigatória!")]
        Guid? disciplinaId,

        [Required(ErrorMessage = "O enunciado é obrigatório!")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "O enunciado deve ter entre 10 e 1000 caracteres!")]
        string enunciado,

        [Required(ErrorMessage = "A alternativa 1 é obrigatória!")]
        [MaxLength(300, ErrorMessage = "A alternativa 1 não pode ultrapassar 300 caracteres!")]
        string alternativa1,

        [Required(ErrorMessage = "A alternativa 2 é obrigatória!")]
        [MaxLength(300, ErrorMessage = "A alternativa 2 não pode ultrapassar 300 caracteres!")]
        string alternativa2,

        [Required(ErrorMessage = "A alternativa 3 é obrigatória!")]
        [MaxLength(300, ErrorMessage = "A alternativa 3 não pode ultrapassar 300 caracteres!")]
        string alternativa3,

        [Required(ErrorMessage = "A alternativa 4 é obrigatória!")]
        [MaxLength(300, ErrorMessage = "A alternativa 4 não pode ultrapassar 300 caracteres!")]
        string alternativa4,

        [Required(ErrorMessage = "A alternativa 5 é obrigatória!")]
        [MaxLength(300, ErrorMessage = "A alternativa 5 não pode ultrapassar 300 caracteres!")]
        string alternativa5,

        [Required(ErrorMessage = "A resposta correta é obrigatória!")]
        string alternativaCorreta,

        [Required(ErrorMessage = "O assunto é obrigatório!")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "O assunto deve ter entre 2 e 100 caracteres!")]
        string assunto,

        [Required(ErrorMessage = "A explicação é obrigatória!")]
        [MaxLength(1000, ErrorMessage = "A explicação não pode ultrapassar 1000 caracteres!")]
        string explicacao,
        [Required(ErrorMessage ="É obrigatórito informar a dificuldade da questão apra que ela possa ser registrada")]
        [EnumDataType(typeof(DificuldadeQuestao),
            ErrorMessage = "A dificuldade informada é inválida!")]
        DificuldadeQuestao? dificuldade
    );
}