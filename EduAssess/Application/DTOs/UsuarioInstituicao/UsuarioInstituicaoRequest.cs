namespace EduAssess.Application.DTOs.UsuarioInstituicao
{
    // ALTERADO: ganhou UnidadeId (opcional), a unidade/prédio em que a pessoa fica lotada.
    // Papel aceita "Aluno", "Professor" ou "Gestor".
    public record UsuarioInstituicaoRequest
        (
            Guid UsuarioId,
            Guid InstituicaoId,
            string Papel,
            Guid? UnidadeId = null
        );
}
