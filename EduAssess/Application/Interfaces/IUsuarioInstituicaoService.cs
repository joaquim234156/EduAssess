namespace EduAssess.Application.Interfaces
{
    public interface IUsuarioInstituicaoService
    {
        Task<bool> VincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
        Task<bool> DesvincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
        Task<IEnumerable<Guid>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<Guid>> ListarUsuariosPorInstituicaoAsync(Guid instituicaoId);
        Task<bool> VerificarVinculoUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
    }
}
