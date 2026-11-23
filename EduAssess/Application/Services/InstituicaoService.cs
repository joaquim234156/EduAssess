using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class InstituicaoService : IInstituicaoService
    {
        private readonly AppDbContext _context;

        public InstituicaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AtivarInstituicaoAsync(Guid id)
        {
            var instituicao = await _context.Instituicoes.FindAsync(id);
            if (instituicao == null) return false;

            // ALTERADO: usa Enable() para também atualizar a data de alteração (antes só trocava o bool).
            instituicao.Enable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InstituicaoResponseDTO> AtualizarInstituicaoAsync(Guid id, InstituicaoRequestPut req)
        {
            var instituicao = await _context.Instituicoes.Include(i => i.Mantenedor).FirstOrDefaultAsync(i => i.Id == id);
            if (instituicao == null)
            {
                throw new KeyNotFoundException("Instituição não encontrada.");
            }

            // NOVO: CNPJ e nome (slug) não podem repetir; antes isso estourava no banco com erro 500.
            await GarantirUnicidadeAsync(req.Cnpj, req.Nome, ignorarId: id);

            instituicao.Update(req.Nome, req.Cnpj, req.Email, req.Telefone, req.Endereco);

            // NOVO: categoria, sigla, mantenedor (nulo = manter) e cores.
            var tipo = req.Tipo is null ? instituicao.Tipo : ParseTipo(req.Tipo);
            var mantenedorId = (req.MantenedorId is null && string.IsNullOrWhiteSpace(req.MantenedorNome))
                ? instituicao.MantenedorId
                : (await ResolverMantenedorAsync(req.MantenedorId, req.MantenedorNome))?.Id;
            instituicao.UpdateClassificacao(tipo, req.Sigla ?? instituicao.Sigla, mantenedorId);
            AplicarCores(instituicao, req.CorPrimaria, req.CorSecundaria);

            await _context.SaveChangesAsync();
            await _context.Entry(instituicao).Reference(i => i.Mantenedor).LoadAsync();
            return ToDto(instituicao);
        }

        public async Task<InstituicaoResponseDTO> CriarInstituicaoAsync(InstituicaoRequest req)
        {
            // NOVO: valida CNPJ e nome (slug) repetidos antes de gravar (antes: erro 500 do banco).
            await GarantirUnicidadeAsync(req.Cnpj, req.Nome, ignorarId: null);

            var tipo = ParseTipo(req.Tipo);
            var mantenedor = await ResolverMantenedorAsync(req.MantenedorId, req.MantenedorNome);
            if (tipo == TipoInstituicao.Colegio && mantenedor is null)
                throw new ArgumentException("O colégio precisa estar vinculado a um mantenedor.");

            var instituicao = new InstituicaoModel(req.Nome, req.Cnpj, req.Email, req.Telefone, req.Endereco, tipo, req.Sigla, mantenedor?.Id);
            AplicarCores(instituicao, req.CorPrimaria, req.CorSecundaria);
            await _context.AddAsync(instituicao);
            await _context.SaveChangesAsync();
            instituicao.Mantenedor = mantenedor;
            return ToDto(instituicao);
        }

        public async Task<bool> DeletarInstituicaoAsync(Guid id)
        {
            var instituicao = await _context.Instituicoes.FindAsync(id);
            if (instituicao == null) return false;

            instituicao.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesAsync()
        {
            var instituicoes = await _context.Instituicoes.Include(i => i.Mantenedor).OrderBy(i => i.CriadaEm).ToListAsync();
            return instituicoes.Select(ToDto);
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesAtivasAsync()
        {
            var instituicoes = await _context.Instituicoes.Include(i => i.Mantenedor)
                .Where(i => i.Ativa).OrderBy(i => i.CriadaEm)
                .ToListAsync();
            return instituicoes.Select(ToDto);
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesInativasAsync()
        {
            var instituicoes = await _context.Instituicoes.Include(i => i.Mantenedor)
                .Where(i => !i.Ativa).OrderBy(i => i.CriadaEm)
                .ToListAsync();
            return instituicoes.Select(ToDto);
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId)
        {
            var instituicoes = await _context.UsuariosInstituicoes
                .Where(ui => ui.UsuarioId == usuarioId && ui.Ativa)
                .Select(ui => ui.Instituicao!)
                .Include(i => i.Mantenedor)
                .ToListAsync();
            return instituicoes.Select(ToDto);
        }

        public async Task<InstituicaoResponseDTO> ObterInstituicaoPorIdAsync(Guid id)
        {
            var instituicao = await _context.Instituicoes.Include(i => i.Mantenedor).FirstOrDefaultAsync(i => i.Id == id);
            if (instituicao == null)
            {
                throw new KeyNotFoundException("Instituição não encontrada.");
            }
            return ToDto(instituicao);
        }

        // ───────────── NOVO: mantenedor, identidade visual e logotipo ─────────────

        public async Task<IEnumerable<MantenedorResponseDTO>> ListarMantenedoresAsync()
        {
            var rows = await _context.Mantenedores.AsNoTracking().Include(m => m.Instituicoes).OrderBy(m => m.Nome).ToListAsync();
            return rows.Select(m => new MantenedorResponseDTO(m.Id, m.Nome,
                m.Instituicoes.OrderBy(i => i.CriadaEm).Select(i => new MantenedorInstituicaoDTO(i.Id, i.Nome, i.Tipo.ToString(), i.Sigla, i.Ativa)).ToList()));
        }

        public async Task<InstituicaoResponseDTO> AtualizarIdentidadeAsync(Guid id, InstituicaoIdentidadeRequest req)
        {
            var instituicao = await _context.Instituicoes.Include(i => i.Mantenedor).FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new KeyNotFoundException("Instituição não encontrada.");
            AplicarCores(instituicao, req.CorPrimaria, req.CorSecundaria);
            await _context.SaveChangesAsync();
            return ToDto(instituicao);
        }

        public async Task<InstituicaoResponseDTO> DefinirLogoAsync(Guid id, bool temLogo)
        {
            var instituicao = await _context.Instituicoes.Include(i => i.Mantenedor).FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new KeyNotFoundException("Instituição não encontrada.");
            instituicao.UpdateBranding(temLogo ? $"{id:N}.webp" : null, instituicao.CorPrimaria, instituicao.CorSecundaria);
            await _context.SaveChangesAsync();
            return ToDto(instituicao);
        }

        // ───────────── auxiliares ─────────────

        private static InstituicaoResponseDTO ToDto(InstituicaoModel i) => new(
            i.Id, i.Nome, i.Cnpj, i.Endereco, i.Telefone, i.Email, i.Ativa,
            i.Tipo.ToString(), i.Sigla, i.MantenedorId, i.Mantenedor?.Nome, i.CorPrimaria, i.CorSecundaria, !string.IsNullOrEmpty(i.LogoPath));

        private static TipoInstituicao ParseTipo(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return TipoInstituicao.Faculdade;
            var normalized = tipo.Trim().Replace("é", "e").Replace("É", "E");
            if (Enum.TryParse<TipoInstituicao>(normalized, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
                return parsed;
            throw new ArgumentException("Tipo inválido. Use Faculdade ou Colegio.");
        }

        private static void AplicarCores(InstituicaoModel instituicao, string? primaria, string? secundaria)
        {
            if (string.IsNullOrWhiteSpace(primaria) && string.IsNullOrWhiteSpace(secundaria)) return;
            var p = (primaria ?? instituicao.CorPrimaria).Trim();
            var s = (secundaria ?? instituicao.CorSecundaria).Trim();
            if (!InstituicaoModel.IsHexColor(p) || !InstituicaoModel.IsHexColor(s))
                throw new ArgumentException("As cores devem estar no formato #RRGGBB (ex.: #1F4E79).");
            instituicao.UpdateBranding(instituicao.LogoPath, p, s);
        }

        /// <summary>Acha o mantenedor por Id ou por nome (sem diferenciar maiúsculas) e cria se o nome for novo.</summary>
        private async Task<MantenedorModel?> ResolverMantenedorAsync(Guid? id, string? nome)
        {
            if (id is Guid g)
                return await _context.Mantenedores.FirstOrDefaultAsync(m => m.Id == g)
                    ?? throw new KeyNotFoundException("Mantenedor não encontrado.");

            var name = (nome ?? "").Trim();
            if (name.Length == 0) return null;
            if (name.Length is < 3 or > 120) throw new ArgumentException("O nome do mantenedor deve ter entre 3 e 120 caracteres.");

            var lower = name.ToLower();
            var existing = await _context.Mantenedores.FirstOrDefaultAsync(m => m.Nome.ToLower() == lower);
            if (existing is not null) return existing;

            var created = new MantenedorModel(name);
            _context.Mantenedores.Add(created);
            return created;
        }

        // NOVO: o banco tem índice único em Cnpj e Slug; aqui damos uma mensagem clara em vez de estourar o erro.
        private async Task GarantirUnicidadeAsync(string cnpj, string nome, Guid? ignorarId)
        {
            var slug = EduAssess.Extensions.StringExtensions.GenerateSlug(nome);
            var repetido = await _context.Instituicoes.AnyAsync(i =>
                (ignorarId == null || i.Id != ignorarId) && (i.Cnpj == cnpj || i.Slug == slug));

            if (repetido)
                throw new ArgumentException("Já existe uma instituição com esse CNPJ ou nome.");
        }
    }
}
