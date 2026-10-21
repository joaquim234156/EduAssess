using EduAssess.Application.DTOs.Nota;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class NotaService : INotaService
    {
        private readonly AppDbContext _context;

        public NotaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<NotaResponseDTO> RegistrarNotaAsync(NotaRequest req, Guid professorId)
        {
            // O professor vem da identidade autenticada; os IDs são passados à entidade na ordem do construtor.
            var nota = new NotaModel(req.idMatricula, professorId, req.idDisciplina, req.descricao, req.valorNota);

            await _context.Notas.AddAsync(nota);
            await _context.SaveChangesAsync();

            return new NotaResponseDTO(
                nota.MatriculaId,
                nota.UsuarioId,
                nota.DisciplinaId,
                nota.Descricao,
                nota.Valor,
                nota.DataLancamento
            );
        }
        public async Task<IEnumerable<NotaResponseDTO>> ObterNotasPorMatriculaAsync(Guid idMatricula)
        {
            return await _context.Notas
                .Where(n => n.MatriculaId == idMatricula)
                .Select(nota => new NotaResponseDTO(
                    nota.MatriculaId,
                    nota.UsuarioId,
                    nota.DisciplinaId,
                    nota.Descricao,
                    nota.Valor,
                    nota.DataLancamento
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<NotaResponseDTO>> ListarTodasAsync()
        {
            return await _context.Notas
                .Select(nota => new NotaResponseDTO(
                    nota.MatriculaId,
                    nota.UsuarioId,
                    nota.DisciplinaId,
                    nota.Descricao,
                    nota.Valor,
                    nota.DataLancamento
                ))
                .ToListAsync();
        }

        public async Task<NotaResponseDTO> AtualizarNotaAsync(Guid id, NotaRequest request)
        {
            var nota = await _context.Notas.FindAsync(id);
            if (nota == null)
            {
                throw new KeyNotFoundException("Nota não encontrada.");
            }
            nota.UpdateNota(request.descricao, request.valorNota);
            await _context.SaveChangesAsync();
            return new NotaResponseDTO(
                nota.MatriculaId,
                nota.UsuarioId,
                nota.DisciplinaId,
                nota.Descricao,
                nota.Valor,
                nota.DataLancamento
            );
        }

        public async Task<bool> DeletarNotaAsync(Guid id)
        {
            var nota = await _context.Notas.FindAsync(id);
            if (nota == null)
            {
                throw new KeyNotFoundException("Nota não encontrada.");
            }
            _context.Notas.Remove(nota);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
