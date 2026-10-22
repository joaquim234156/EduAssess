using EduAssess.Application.DTOs.Matricula;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class MatriculaService : IMatriculaService
    {
        private readonly AppDbContext _context;

        public MatriculaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MatriculaResponseDTO> AtualizarMatriculaAsync(Guid id, MatriculaRequestPut req)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null)
            {
                throw new KeyNotFoundException("Matrícula não encontrada.");
            }

            matricula.Update(req.usuarioId, req.turmaId);
            await _context.SaveChangesAsync();

            return new MatriculaResponseDTO
            (
                matricula.Id,
                matricula.UsuarioId,
                matricula.TurmaId,
                matricula.DataMatricula,
                matricula.Ativa
            );
        }

        public async Task<MatriculaResponseDTO> CriarMatriculaAsync(MatriculaRequest req)
        {
            var matricula = new MatriculaModel(req.usuarioId, req.turmaId);

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            return new MatriculaResponseDTO
            (
                matricula.Id,
                matricula.UsuarioId,
                matricula.TurmaId,
                matricula.DataMatricula,
                matricula.Ativa
            );
        }

        public async Task<bool> DeletarMatriculaAsync(Guid id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null)
            {
                throw new KeyNotFoundException("Matrícula não encontrada.");
            }

            matricula.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MatriculaResponseDTO>> ListarMatriculasAsync()
        {
            return await _context.Matriculas
                .AsNoTracking()
                .OrderByDescending(m => m.DataMatricula)
                .Select(m => new MatriculaResponseDTO(
                    m.Id,
                    m.UsuarioId,
                    m.TurmaId,
                    m.DataMatricula,
                    m.Ativa
                ))
                .ToListAsync();
        }


        public async Task<MatriculaResponseDTO> ObterMatriculaPorIdAsync(Guid id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null)
            {
                throw new KeyNotFoundException("Matrícula não encontrada.");
            }

            return new MatriculaResponseDTO(
                matricula.Id,
                matricula.UsuarioId,
                matricula.TurmaId,
                matricula.DataMatricula,
                matricula.Ativa
            );
        }
    }
}
