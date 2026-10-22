namespace EduAssess.Application.DTOs.Nota
{
    public record NotaResponseDTO
    (
        Guid Id,
        Guid IdMatricula,
        Guid IdUsuario,
        Guid IdDisciplina,
        string descricao,
        decimal ValorNota,
        DateTime DataLancamento
    );
}
