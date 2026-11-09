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

        // ALTERADO: antes não validava nada. Se o usuário, a instituição ou a unidade não existissem, ou
        // se o vínculo já existisse (a chave é composta), a rota respondia 500. Agora valida e responde com
        // uma mensagem clara; e, se já houve um vínculo encerrado, ele é reativado.
        public async Task<UsuarioInstituicaoResponseDTO> VincularUsuarioInstituicaoAsync(UsuarioInstituicaoRequest req)
        {
            var papel = ParsePapel(req.Papel);
            await ValidarReferenciasAsync(req.UsuarioId, req.InstituicaoId, req.UnidadeId);

            var existente = await _context.UsuariosInstituicoes
                .FirstOrDefaultAsync(ui => ui.UsuarioId == req.UsuarioId && ui.InstituicaoId == req.InstituicaoId);

            if (existente is null)
            {
                existente = new UsuarioInstituicaoModel(req.UsuarioId, req.InstituicaoId, papel, req.UnidadeId);
                await _context.UsuariosInstituicoes.AddAsync(existente);
            }
            else if (existente.Ativa)
            {
                throw new InvalidOperationException("Esta pessoa já está vinculada à instituição. Use a atualização para mudar papel ou unidade.");
            }
            else
            {
                existente.Reativar(papel, req.UnidadeId);
            }

            await _context.SaveChangesAsync();
            return ToResponse(existente);
        }

        // NOVO: muda papel e/ou unidade (prédio) de quem já está vinculado.
        public async Task<UsuarioInstituicaoResponseDTO> AtualizarUsuarioInstituicaoAsync(UsuarioInstituicaoRequestPut req)
        {
            var papel = ParsePapel(req.Papel);
            await ValidarReferenciasAsync(req.UsuarioId, req.InstituicaoId, req.UnidadeId);

            var vinculo = await _context.UsuariosInstituicoes
                .FirstOrDefaultAsync(ui => ui.UsuarioId == req.UsuarioId && ui.InstituicaoId == req.InstituicaoId && ui.Ativa)
                ?? throw new KeyNotFoundException("Vínculo não encontrado.");

            vinculo.Update(papel, req.UnidadeId);
            await _context.SaveChangesAsync();
            return ToResponse(vinculo);
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

        // ALTERADO: só devolve vínculos ATIVOS (antes listava também quem já havia sido desvinculado).
        public async Task<IEnumerable<Guid>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.UsuariosInstituicoes
                .Where(ui => ui.UsuarioId == usuarioId && ui.Ativa)
                .Select(ui => ui.InstituicaoId)
                .ToListAsync();
        }

        // ALTERADO: idem, só vínculos ativos.
        public async Task<IEnumerable<Guid>> ListarUsuariosPorInstituicaoAsync(Guid instituicaoId)
        {
            return await _context.UsuariosInstituicoes
                .Where(ui => ui.InstituicaoId == instituicaoId && ui.Ativa)
                .Select(ui => ui.UsuarioId)
                .ToListAsync();
        }

        public async Task<bool> VerificarVinculoUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId)
        {
            return await _context.UsuariosInstituicoes
                .AnyAsync(ui => ui.UsuarioId == usuarioId && ui.InstituicaoId == instituicaoId && ui.Ativa);
        }

        // ───────── auxiliares ─────────

        // ALTERADO: antes usava Enum.Parse, que lançava exceção (500) com um papel inválido.
        private static PapelInstituicao ParsePapel(string? papel)
        {
            if (Enum.TryParse<PapelInstituicao>(papel?.Trim(), ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            throw new ArgumentException("Papel inválido. Use Aluno, Professor ou Gestor.");
        }

        private async Task ValidarReferenciasAsync(Guid usuarioId, Guid instituicaoId, Guid? unidadeId)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == usuarioId && u.Ativa))
                throw new KeyNotFoundException("Usuário não encontrado ou inativo.");

            if (!await _context.Instituicoes.AnyAsync(i => i.Id == instituicaoId && i.Ativa))
                throw new KeyNotFoundException("Instituição não encontrada ou inativa.");

            if (unidadeId.HasValue
                && !await _context.Unidades.AnyAsync(u => u.Id == unidadeId.Value && u.InstituicaoId == instituicaoId && u.Ativa))
            {
                throw new ArgumentException("A unidade informada não existe, está inativa ou não pertence a esta instituição.");
            }
        }

        private static UsuarioInstituicaoResponseDTO ToResponse(UsuarioInstituicaoModel m) => new(
            m.UsuarioId, m.InstituicaoId, m.Papel.ToString(), m.VinculadoEm, m.DesvinculoEm, m.UnidadeId);
    }
}
