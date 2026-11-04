using EduAssess.Application.DTOs.Unidade;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class UnidadeService : IUnidadeService
    {
        private readonly AppDbContext _context;

        public UnidadeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AtivarUnidadeAsync(Guid id)
        {
            var unidade = await _context.Unidades.FindAsync(id);
            if (unidade == null) return false;

            unidade.Enable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UnidadeResponseDTO> AtualizarUnidadeAsync(Guid id, UnidadeRequestPut req)
        {
            var unidade = await _context.Unidades.FindAsync(id);
            if (unidade == null)
            {
                throw new KeyNotFoundException("Unidade não encontrada.");
            }
            unidade.Update(req.Nome, req.Tipo, req.Sigla);
            await _context.SaveChangesAsync();
            return new UnidadeResponseDTO(unidade.Id, unidade.InstituicaoId, unidade.Nome, unidade.Tipo, unidade.Sigla);
        }

        public async Task<UnidadeResponseDTO> CriarUnidadeAsync(UnidadeRequest req)
        {
            var unidade = new UnidadeModel(req.IdInstituicao, req.Nome, req.Sigla, req.Tipo);
            await _context.Unidades.AddAsync(unidade);
            await _context.SaveChangesAsync();
            return new UnidadeResponseDTO(unidade.Id, unidade.InstituicaoId, unidade.Nome, unidade.Tipo, unidade.Sigla);
        }

        public async Task<bool> DeletarUnidadeAsync(Guid id)
        {
            var unidade = await _context.Unidades.FindAsync(id);
            if (unidade == null)
            {
                throw new KeyNotFoundException("Unidade não encontrada.");
            }
            unidade.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAtivasAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.Ativa)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesInativasAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => !u.Ativa)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoAsync(Guid instituicaoId)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.InstituicaoId == instituicaoId)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoETipoAsync(Guid instituicaoId, TipoUnidade tipo)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.InstituicaoId == instituicaoId && u.Tipo == tipo)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorTipoAsync(TipoUnidade tipo)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.Tipo == tipo)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => new UnidadeResponseDTO(
                u.Id,
                u.InstituicaoId,
                u.Nome,
                u.Tipo,
                u.Sigla
            )).ToList();
        }

        public async Task<UnidadeResponseDTO> ObterUnidadePorIdAsync(Guid id)
        {
            var unidade = await _context.Unidades.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (unidade == null)
            {
                throw new KeyNotFoundException("Unidade não encontrada.");
            }
            return new UnidadeResponseDTO(
                unidade.Id,
                unidade.InstituicaoId,
                unidade.Nome,
                unidade.Tipo,
                unidade.Sigla
            );
        }
    }
}
