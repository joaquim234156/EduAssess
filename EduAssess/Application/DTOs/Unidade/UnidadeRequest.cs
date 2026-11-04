using EduAssess.Models.Global;

namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeRequest
        (
            Guid IdInstituicao,
            string Nome,
            TipoUnidade Tipo,
            string Sigla
        );
}
