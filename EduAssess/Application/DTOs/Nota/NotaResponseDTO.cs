namespace EduAssess.Application.DTOs.Nota
{
    public record NotaResponseDTO
    (
        Guid IdMatricula,
        Guid IdUsuario,
        Guid IdDisciplina,
        string descricao,
        decimal ValorNota,
        DateTime DataLancamento
    );
}
