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
            if (!Enum.IsDefined(req.Tipo))
                throw new ArgumentException("Tipo de unidade inválido.");

            // Nome e sigla não podem repetir dentro da mesma instituição.
            await GarantirUnicidadeAsync(unidade.InstituicaoId, req.Nome, req.Sigla, ignorarId: id);

            unidade.Update(req.Nome, req.Tipo, req.Sigla);
            await _context.SaveChangesAsync();
            return ToDto(unidade);
        }

        public async Task<UnidadeResponseDTO> CriarUnidadeAsync(UnidadeRequest req)
        {
            if (!Enum.IsDefined(req.Tipo))
                throw new ArgumentException("Tipo de unidade inválido.");

            // A instituição precisa existir e nome/sigla não podem repetir.
            if (!await _context.Instituicoes.AnyAsync(i => i.Id == req.IdInstituicao && i.Ativa))
                throw new KeyNotFoundException("Instituição não encontrada ou inativa.");
            await GarantirUnicidadeAsync(req.IdInstituicao, req.Nome, req.Sigla, ignorarId: null);

            var unidade = new UnidadeModel(req.IdInstituicao, req.Nome, req.Sigla, req.Tipo);
            await _context.Unidades.AddAsync(unidade);
            await _context.SaveChangesAsync();
            return ToDto(unidade);
        }

        public async Task<bool> DeletarUnidadeAsync(Guid id)
        {
            var unidade = await _context.Unidades.FindAsync(id);
            if (unidade == null)
            {
                throw new KeyNotFoundException("Unidade não encontrada.");
            }
            // NOVO: não dá para desativar um prédio que ainda tem gente lotada; mova as pessoas antes.
            var lotados = await _context.UsuariosInstituicoes.CountAsync(ui => ui.UnidadeId == id && ui.Ativa);
            if (lotados > 0)
                throw new InvalidOperationException($"A unidade ainda tem {lotados} pessoa(s) lotada(s). Mova-as para outra unidade antes de desativar.");

            unidade.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAtivasAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.Ativa)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesInativasAsync()
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => !u.Ativa)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoAsync(Guid instituicaoId)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.InstituicaoId == instituicaoId)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoETipoAsync(Guid instituicaoId, TipoUnidade tipo)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.InstituicaoId == instituicaoId && u.Tipo == tipo)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorTipoAsync(TipoUnidade tipo)
        {
            var unidades = await _context.Unidades.AsNoTracking()
                .Where(u => u.Tipo == tipo)
                .OrderBy(u => u.Nome)
                .ToListAsync();
            return unidades.Select(u => ToDto(u)).ToList();
        }

        public async Task<UnidadeResponseDTO> ObterUnidadePorIdAsync(Guid id)
        {
            var unidade = await _context.Unidades.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (unidade == null)
            {
                throw new KeyNotFoundException("Unidade não encontrada.");
            }
            return ToDto(unidade);
        }

        public async Task<UnidadeResponseDTO> AtualizarIdentidadeAsync(Guid id, string? corPrimaria, string? corSecundaria)
        {
            var unidade = await _context.Unidades.FindAsync(id)
                ?? throw new KeyNotFoundException("Unidade não encontrada.");

            if (corPrimaria is not null && !InstituicaoModel.IsHexColor(corPrimaria))
                throw new ArgumentException("A cor primária deve estar no formato #RRGGBB.");
            if (corSecundaria is not null && !InstituicaoModel.IsHexColor(corSecundaria))
                throw new ArgumentException("A cor secundária deve estar no formato #RRGGBB.");

            unidade.UpdateBranding(unidade.LogoPath, corPrimaria, corSecundaria);
            await _context.SaveChangesAsync();
            return ToDto(unidade);
        }

        public async Task<UnidadeResponseDTO> DefinirLogoAsync(Guid id, bool temLogo)
        {
            var unidade = await _context.Unidades.FindAsync(id)
                ?? throw new KeyNotFoundException("Unidade não encontrada.");
            unidade.UpdateBranding(temLogo ? $"{id:N}.webp" : null, unidade.CorPrimaria, unidade.CorSecundaria);
            await _context.SaveChangesAsync();
            return ToDto(unidade);
        }

        private static UnidadeResponseDTO ToDto(UnidadeModel unidade) => new(
            unidade.Id, unidade.InstituicaoId, unidade.Nome, unidade.Tipo, unidade.Sigla,
            unidade.LogoPath, unidade.CorPrimaria, unidade.CorSecundaria);

        // NOVO: regra de negócio com mensagem clara (o índice único do banco só falharia no SaveChanges).
        private async Task GarantirUnicidadeAsync(Guid instituicaoId, string nome, string sigla, Guid? ignorarId)
        {
            var slug = EduAssess.Extensions.StringExtensions.GenerateSlug(nome);
            var siglaNormalizada = (sigla ?? "").Trim().ToUpper();

            var repetida = await _context.Unidades.AnyAsync(u =>
                u.InstituicaoId == instituicaoId
                && (ignorarId == null || u.Id != ignorarId)
                && (u.Slug == slug || (u.Sigla ?? "").ToUpper() == siglaNormalizada));

            if (repetida)
                throw new ArgumentException("Já existe uma unidade com esse nome ou sigla nesta instituição.");
        }
    }
}
