namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    // ALTERADO: ganhou UnidadeId no fim (com valor padrão, então quem já usava o DTO não quebra).
    public record UsuarioInstituicaoResponseDTO
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel,
            DateTime DataVinculo,
            DateTime? DataDesvinculo,
            Guid? UnidadeId = null
        );
}
