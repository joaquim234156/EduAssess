namespace EduAssess.Application.DTOs.Curso
{
    public record CursoResponseDTO
        (
        Guid Id,
        string Nome,
        string Descricao,
        int DuracaoEmAnos,
        bool Ativo
        );
}
