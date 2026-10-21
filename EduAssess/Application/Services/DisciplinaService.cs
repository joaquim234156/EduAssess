using EduAssess.Application.DTOs.Curso;
using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    public class DisciplinaService : IDisciplinaService
    {
        private readonly AppDbContext _context;

        public DisciplinaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DisciplinaResponseDTO> CriarDisciplinaAsync(DisciplinaRequest req, Guid professorId)
        {
            var disciplina = new DisciplinaModel(req.nome, req.descricao, professorId);
            await _context.Disciplinas.AddAsync(disciplina);
            await _context.SaveChangesAsync();
            return new DisciplinaResponseDTO(
                disciplina.Id,
                disciplina.Nome,
                disciplina.Descricao
            );
        }

        public async Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasAsync()
        {
            return await _context.Disciplinas
                .Select(disciplina => new DisciplinaResponseDTO(
                    disciplina.Id,
                    disciplina.Nome,
                    disciplina.Descricao
                ))
                .ToListAsync();
        }


        public async Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasAtivasAsync()
        {
            return await _context.Disciplinas
                .Where(disciplina => disciplina.Ativa)
                .Select(disciplina => new DisciplinaResponseDTO(
                    disciplina.Id,
                    disciplina.Nome,
                    disciplina.Descricao
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasInativasAsync()
        {
            return await _context.Disciplinas
                .Where(disciplina => !disciplina.Ativa)
                .Select(disciplina => new DisciplinaResponseDTO(
                    disciplina.Id,
                    disciplina.Nome,
                    disciplina.Descricao
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasPorProfessorAsync(Guid professorId)
        {
            return await _context.Disciplinas
                .Where(disciplina => disciplina.ProfessorId == professorId)
                .Select(disciplina => new DisciplinaResponseDTO(
                    disciplina.Id,
                    disciplina.Nome,
                    disciplina.Descricao
                ))
                .ToListAsync();
        }

        public async Task<DisciplinaResponseDTO> ObterDisciplinaPorIdAsync(Guid id)
        {
            var disciplina = await _context.Disciplinas.FindAsync(id);
            if (disciplina == null)
            {
                throw new KeyNotFoundException($"Disciplina com ID {id} não encontrada.");
            }
            return new DisciplinaResponseDTO(
                disciplina.Id,
                disciplina.Nome,
                disciplina.Descricao
            );
        }

        public async Task<DisciplinaResponseDTO> AtualizarDisciplinaAsync(Guid id, DisciplinaRequestPut req)
        {
            var disciplina = await _context.Disciplinas.FindAsync(id);
            if (disciplina == null)
            {
                throw new KeyNotFoundException("Disciplina não encontrada.");
            }

            disciplina.UpdateDisciplina(req.nome, req.descricao, req.professorId);

            await _context.SaveChangesAsync();
            return new DisciplinaResponseDTO(
                disciplina.Id,
                disciplina.Nome,
                disciplina.Descricao
            );
        }

        public async Task<bool> DeletarDisciplinaAsync(Guid id)
        {
            var disciplina = await _context.Disciplinas.FindAsync(id);
            if (disciplina == null)
            {
                throw new KeyNotFoundException("Disciplina não encontrada.");
            }
            disciplina.Disable();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AtivarDisciplinaAsync(Guid id)
        {
            var disciplina = await _context.Disciplinas.FindAsync(id);
            if (disciplina == null)
            {
                throw new KeyNotFoundException("Disciplina não encontrada.");
            }
            disciplina.Enable();
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
