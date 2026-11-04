using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class UsuarioInstituicaoService : IUsuarioInstituicaoService
    {
        private readonly AppDbContext _context;

        public UsuarioInstituicaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DesvincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId)
        {
            var usuarioInstituicao = await _context.UsuariosInstituicoes
                .FirstOrDefaultAsync(ui => ui.UsuarioId == usuarioId && ui.InstituicaoId == instituicaoId);

            if (usuarioInstituicao == null)
            {
                return false;
            }

            usuarioInstituicao.Desvincular();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Guid>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.UsuariosInstituicoes
                .Where(ui => ui.UsuarioId == usuarioId)
                .Select(ui => ui.InstituicaoId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Guid>> ListarUsuariosPorInstituicaoAsync(Guid instituicaoId)
        {
            return await _context.UsuariosInstituicoes
                .Where(ui => ui.InstituicaoId == instituicaoId)
                .Select(ui => ui.UsuarioId)
                .ToListAsync();
        }

        public async Task<bool> VerificarVinculoUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId)
        {
            return await _context.UsuariosInstituicoes
                .AnyAsync(ui => ui.UsuarioId == usuarioId && ui.InstituicaoId == instituicaoId);
        }

        public async Task<UsuarioInstituicaoResponseDTO> VincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId, PapelInstituicao papel)
        {
            var usuarioInstituicao = new UsuarioInstituicaoModel(usuarioId, instituicaoId, papel);

            await _context.UsuariosInstituicoes.AddAsync(usuarioInstituicao);
            await _context.SaveChangesAsync();
            return new UsuarioInstituicaoResponseDTO(usuarioInstituicao.UsuarioId, usuarioInstituicao.InstituicaoId, usuarioInstituicao.Papel.ToString(), usuarioInstituicao.VinculadoEm, usuarioInstituicao.DesvinculoEm);
        }
    }
}
