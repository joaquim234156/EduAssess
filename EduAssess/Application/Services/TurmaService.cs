using EduAssess.Application.DTOs.Turma;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class TurmaService : ITurmaService
    {
        private readonly AppDbContext _context;

        public TurmaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AtivarTurmaAsync(Guid id)
        {
            var turma = await _context.Turmas.FindAsync(id);
            if (turma == null) return false;

            turma.Enable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TurmaResponseDTO> AtualizarTurmaAsync(Guid id, TurmaRequestPut req)
        {
            var turma = await _context.Turmas.FindAsync(id);
            if (turma == null) return null;

            turma.Update(req.nome, req.professorId, req.cursoId, req.periodo);
            await _context.SaveChangesAsync();
            return new TurmaResponseDTO(turma.Id, turma.Nome, turma.ProfessorId, turma.CursoId, turma.Periodo);
        }

        public async Task<TurmaResponseDTO> CriarTurmaAsync(TurmaRequest req)
        {
            var turma = new TurmaModel(req.nome, req.professorId, req.cursoId);
            _context.Turmas.Add(turma);
            await _context.SaveChangesAsync();
            return new TurmaResponseDTO(turma.Id, turma.Nome, turma.ProfessorId, turma.CursoId, turma.Periodo);
        }

        public async Task<bool> DeletarTurmaAsync(Guid id)
        {
            var turma = await _context.Turmas.FindAsync(id);
            if (turma == null) return false;

            turma.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TurmaResponseDTO>> ListarTurmasAsync()
        {
            var turmas = await _context.Turmas.ToListAsync();
            return turmas.Select(t => new TurmaResponseDTO(t.Id, t.Nome, t.ProfessorId, t.CursoId, t.Periodo));
        }

        public async Task<IEnumerable<TurmaResponseDTO>> ListarTurmasPorProfessorAsync(Guid professorId)
        {
            var turmas = await _context.Turmas.Where(t => t.ProfessorId == professorId).ToListAsync();
            return turmas.Select(t => new TurmaResponseDTO(t.Id, t.Nome, t.ProfessorId, t.CursoId, t.Periodo));
        }

        public async Task<TurmaResponseDTO> ObterTurmaPorIdAsync(Guid id)
        {
            var turma = await _context.Turmas.FindAsync(id);
            if (turma == null) return null;

            return new TurmaResponseDTO(turma.Id, turma.Nome, turma.ProfessorId, turma.CursoId, turma.Periodo);
        }
    }
}
