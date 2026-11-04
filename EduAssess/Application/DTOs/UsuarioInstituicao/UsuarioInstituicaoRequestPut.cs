namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    public record UsuarioInstituicaoRequestPut
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel
        );
}
