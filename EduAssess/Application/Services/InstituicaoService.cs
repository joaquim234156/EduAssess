using EduAssess.Application.DTOs.Curso;
using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
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
            var instituicao = await _context.Instituicoes.FindAsync(id);
            if (instituicao == null)
            {
                throw new KeyNotFoundException("Instituição não encontrada.");
            }

            // NOVO: CNPJ e nome (slug) não podem repetir; antes isso estourava no banco com erro 500.
            await GarantirUnicidadeAsync(req.Cnpj, req.Nome, ignorarId: id);

            instituicao.Update(req.Nome, req.Cnpj, req.Email, req.Telefone, req.Endereco);

            await _context.SaveChangesAsync();
            return new InstituicaoResponseDTO(
                instituicao.Id, instituicao.Nome, instituicao.Cnpj, instituicao.Endereco, instituicao.Telefone, instituicao.Email, instituicao.Ativa);
        }

        public async Task<InstituicaoResponseDTO> CriarInstituicaoAsync(InstituicaoRequest req)
        {
            // NOVO: valida CNPJ e nome (slug) repetidos antes de gravar (antes: erro 500 do banco).
            await GarantirUnicidadeAsync(req.Cnpj, req.Nome, ignorarId: null);

            var instituicao = new InstituicaoModel(req.Nome, req.Cnpj, req.Email, req.Telefone, req.Endereco);
            await _context.AddAsync(instituicao);
            await _context.SaveChangesAsync();
            return new InstituicaoResponseDTO(
                instituicao.Id, instituicao.Nome, instituicao.Cnpj, instituicao.Endereco, instituicao.Telefone, instituicao.Email, instituicao.Ativa);
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
            var instituicoes = await _context.Instituicoes.ToListAsync();
            return instituicoes.Select(i => new InstituicaoResponseDTO(
                i.Id, i.Nome, i.Cnpj, i.Endereco, i.Telefone, i.Email, i.Ativa));
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesAtivasAsync()
        {
            var instituicoes = await _context.Instituicoes
                .Where(i => i.Ativa)
                .ToListAsync();
            return instituicoes.Select(i => new InstituicaoResponseDTO(
                i.Id, i.Nome, i.Cnpj, i.Endereco, i.Telefone, i.Email, i.Ativa));
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesInativasAsync()
        {
            var instituicoes = await _context.Instituicoes
                .Where(i => !i.Ativa)
                .ToListAsync();
            return instituicoes.Select(i => new InstituicaoResponseDTO(
                i.Id, i.Nome, i.Cnpj, i.Endereco, i.Telefone, i.Email, i.Ativa));
        }

        public async Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId)
        {
            var instituicoes = await _context.UsuariosInstituicoes
                .Where(ui => ui.UsuarioId == usuarioId && ui.Ativa)
                .Select(ui => ui.Instituicao)
                .ToListAsync();
            return instituicoes.Select(i => new InstituicaoResponseDTO(
                i.Id, i.Nome, i.Cnpj, i.Endereco, i.Telefone, i.Email, i.Ativa));
        }

        public async Task<InstituicaoResponseDTO> ObterInstituicaoPorIdAsync(Guid id)
        {
            var instituicao = await _context.Instituicoes.FindAsync(id);
            if (instituicao == null)
            {
                throw new KeyNotFoundException("Instituição não encontrada.");
            }
            return new InstituicaoResponseDTO(
                instituicao.Id, instituicao.Nome, instituicao.Cnpj, instituicao.Endereco, instituicao.Telefone, instituicao.Email, instituicao.Ativa);
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
