using EduAssess.Application.DTOs.Curso;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class CursoService : ICursoService
    {
        private readonly AppDbContext _context;

        public CursoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CursoResponseDTO> CriarCursoAsync(CursoRequest req)
        {
            var curso = new CursoModel(req.nome, req.descricao, req.duracaoEmAnos);
            await _context.Cursos.AddAsync(curso);
            await _context.SaveChangesAsync();

            return new CursoResponseDTO(
                curso.Id,
                curso.Nome,
                curso.Descricao,
                curso.DuracaoEmAnos,
                curso.Ativo
            );
        }

        public async Task<IEnumerable<CursoResponseDTO>> ListarTodosAsync()
        {
            return await _context.Cursos
                .Select(curso => new CursoResponseDTO(
                    curso.Id,
                    curso.Nome,
                    curso.Descricao,
                    curso.DuracaoEmAnos,
                    curso.Ativo
                ))
                .ToListAsync();
        }

        public async Task<CursoResponseDTO> ObterCursoPorIdAsync(Guid id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                throw new KeyNotFoundException($"Curso com ID {id} não encontrado.");
            }
            return new CursoResponseDTO(
                curso.Id,
                curso.Nome,
                curso.Descricao,
                curso.DuracaoEmAnos,
                curso.Ativo
            );
        }

        public async Task<CursoResponseDTO> AtualizarCursoAsync(Guid id, CursoRequest req)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                throw new KeyNotFoundException("Curso não encontrado.");
            }
            curso.Update(req.nome, req.descricao, req.duracaoEmAnos);
            await _context.SaveChangesAsync();
            return new CursoResponseDTO(
                curso.Id,
                curso.Nome,
                curso.Descricao,
                curso.DuracaoEmAnos,
                curso.Ativo
            );
        }
    }
}
