namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    public record UsuarioInstituicaoRequest
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel
        );
}
