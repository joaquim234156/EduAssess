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

        public async Task<NotaResponseDTO> RegistrarNotaAsync(NotaRequest req)
        {
            var nota = new NotaModel(req.idMatricula, req.idDisciplina, req.idUsuario, req.descricao, req.valorNota);

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
    }
}
