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
            var nomeValido = req.nome?.Trim();
            if (string.IsNullOrWhiteSpace(nomeValido))
                throw new ArgumentException("O nome da disciplina é obrigatório.");

            if (await _context.Disciplinas.AnyAsync(d => d.Ativa && d.Nome == nomeValido))
                throw new ArgumentException("Já existe uma disciplina ativa com esse nome.");

            if (!await _context.Users.AnyAsync(u => u.Id == professorId && u.Ativa && u.Role == UserRole.Professor))
                throw new KeyNotFoundException("O professor responsável informado não foi encontrado ou está inativo.");

            var disciplina = new DisciplinaModel(
                nomeValido,
                req.descricao ?? string.Empty,
                professorId
            );

            await _context.Disciplinas.AddAsync(disciplina);
            await _context.SaveChangesAsync();

            return new DisciplinaResponseDTO(
                disciplina.Id,
                disciplina.Nome,
                disciplina.Descricao,
                disciplina.ProfessorId,
                disciplina.Ativa
            );
        }

        public async Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasAsync()
        {
            return await _context.Disciplinas
                .Select(disciplina => new DisciplinaResponseDTO(
                    disciplina.Id,
                    disciplina.Nome,
                    disciplina.Descricao,
                    disciplina.ProfessorId,
                    disciplina.Ativa
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
                    disciplina.Descricao,
                    disciplina.ProfessorId,
                    disciplina.Ativa
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
                    disciplina.Descricao,
                    disciplina.ProfessorId,
                    disciplina.Ativa
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
                    disciplina.Descricao,
                    disciplina.ProfessorId,
                    disciplina.Ativa
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
                disciplina.Descricao,
                disciplina.ProfessorId,
                disciplina.Ativa
            );
        }

        public async Task<DisciplinaResponseDTO> AtualizarDisciplinaAsync(Guid id, DisciplinaRequestPut req)
        {
            var disciplina = await _context.Disciplinas.FindAsync(id);
            if (disciplina == null)
            {
                throw new KeyNotFoundException("Disciplina não encontrada.");
            }

            disciplina.UpdateDisciplina(req.nome, req.descricao ?? string.Empty, req.professorId);

            await _context.SaveChangesAsync();
            return new DisciplinaResponseDTO(
                disciplina.Id,
                disciplina.Nome,
                disciplina.Descricao,
                disciplina.ProfessorId,
                disciplina.Ativa
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
