using EduAssess.Models.Global;

namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeRequestPut(
        string Nome,
        string Sigla,
        TipoUnidade Tipo
        );
}
