namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    public record UsuarioInstituicaoResponseDTO
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel,
            DateTime DataVinculo,
            DateTime? DataDesvinculo
        );
}
