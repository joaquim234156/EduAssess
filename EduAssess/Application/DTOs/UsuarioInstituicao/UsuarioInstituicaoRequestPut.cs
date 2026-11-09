namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    // ALTERADO: ganhou UnidadeId (opcional) para mover a pessoa de prédio.
    public record UsuarioInstituicaoRequestPut
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel,
            Guid? UnidadeId = null
        );
}
