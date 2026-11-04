using EduAssess.Models.Global;

namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeResponseDTO(
        Guid Id,
        Guid InstituicaoId,
        string Nome,
        TipoUnidade Tipo,
        string Sigla
    );
}
