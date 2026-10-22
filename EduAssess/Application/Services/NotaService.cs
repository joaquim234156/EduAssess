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
            if (req.idMatricula == Guid.Empty)
                throw new ArgumentException("Selecione a matrícula do aluno.");
            if (req.idDisciplina == Guid.Empty)
                throw new ArgumentException("Selecione uma disciplina.");
            if (req.idUsuario == Guid.Empty)
                throw new ArgumentException("Informe o professor responsável pelo lançamento.");
            if (req.valorNota < 0 || req.valorNota > 10)
                throw new ArgumentException("A nota deve estar entre 0 e 10.");
            if (string.IsNullOrWhiteSpace(req.descricao))
                throw new ArgumentException("Informe a descrição da avaliação.");

            if (!await _context.Matriculas.AnyAsync(m => m.Id == req.idMatricula && m.Ativa))
                throw new KeyNotFoundException("Matrícula ativa não encontrada.");
            if (!await _context.Disciplinas.AnyAsync(d => d.Id == req.idDisciplina && d.Ativa))
                throw new KeyNotFoundException("Disciplina ativa não encontrada.");
            if (!await _context.Users.AnyAsync(u => u.Id == req.idUsuario && u.Ativa
                    && (u.Role == UserRole.Professor || u.Role == UserRole.Gestor || u.Role == UserRole.Admin)))
                throw new KeyNotFoundException("O usuário informado não pode lançar notas.");

            // Ordem do construtor: matrícula, usuário (quem lançou), disciplina.
            var nota = new NotaModel(
                req.idMatricula,
                req.idUsuario,
                req.idDisciplina,
                req.descricao.Trim(),
                decimal.Round(req.valorNota, 2));

            await _context.Notas.AddAsync(nota);
            await _context.SaveChangesAsync();

            return ToDto(nota);
        }

        public async Task<IEnumerable<NotaResponseDTO>> ObterNotasPorMatriculaAsync(Guid idMatricula)
        {
            var notas = await _context.Notas.AsNoTracking()
                .Where(n => n.MatriculaId == idMatricula)
                .OrderByDescending(n => n.DataLancamento)
                .ToListAsync();
            return notas.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<NotaResponseDTO>> ObterNotasDoAlunoAsync(Guid idAluno)
        {
            var notas = await _context.Notas.AsNoTracking()
                .Where(n => n.Matricula.UsuarioId == idAluno)
                .OrderByDescending(n => n.DataLancamento)
                .ToListAsync();
            return notas.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<NotaResponseDTO>> ListarTodasAsync()
        {
            var notas = await _context.Notas.AsNoTracking()
                .OrderByDescending(n => n.DataLancamento)
                .ToListAsync();
            return notas.Select(ToDto).ToList();
        }

        private static NotaResponseDTO ToDto(NotaModel nota) => new(
            nota.Id,
            nota.MatriculaId,
            nota.UsuarioId,
            nota.DisciplinaId,
            nota.Descricao,
            nota.Valor,
            nota.DataLancamento);
    }
}
