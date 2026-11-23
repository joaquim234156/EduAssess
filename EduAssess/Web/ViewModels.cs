using EduAssess.Application.DTOs.Selo;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;

namespace EduAssess.Web;

public sealed record PersonRow(Guid Id, string Name, string Email, string Phone);
public sealed record StaffRow(Guid Id, string Name, string Email, UserRole Role);
public sealed record InstructorRow(Guid Id, string Name, bool Active = true);
public sealed record CourseRow(Guid Id, string Name, string Description, int Duration, int ClassCount, bool Active);
public sealed record DisciplineRow(Guid Id, string Name, string Description, Guid TeacherId, string TeacherName, bool Active);
public sealed record ClassRow(Guid Id, string Name, string CourseName, string InstructorName, int Period, int StudentCount, Guid CourseId, Guid InstructorId, bool Active);
public sealed record EnrollmentRow(Guid Id, string StudentName, string ClassName, string CourseName, DateTime EnrolledAt, bool Active, Guid StudentId = default, Guid ClassId = default);
public sealed record GradeRow(Guid Id, string StudentName, string ClassName, string DisciplineName, string Description, decimal Value, DateTime CreatedAt, string LaunchedBy);
public sealed record EnrollmentOption(Guid Id, string Label);
public sealed record DisciplineOption(Guid Id, string Name);
public sealed record CoursePerformance(string CourseName, decimal? Average, int GradeCount);
public sealed record ContactRow(Guid Id, string Name, string Email, string Phone, string Institution, string Role, string Message, DateTime CreatedAt, bool Read);

public sealed record DashboardSummary(
    int CourseCount, int ClassCount, int StudentCount, int TeacherCount, int EnrollmentCount,
    int PendingAccounts, int GradeCount, decimal? AverageGrade, int UnreadContacts,
    List<EnrollmentRow> RecentEnrollments, List<CoursePerformance> Performance);

public sealed record MyEnrollmentRow(string ClassName, string CourseName, string TeacherName, DateTime EnrolledAt, DateTime ExpiresAt, bool Active);
public sealed record MyGradeRow(string DisciplineName, string ClassName, string Description, decimal Value, DateTime CreatedAt);
public sealed record DisciplineAverage(string DisciplineName, decimal Average, int Count);
public sealed record StudentOverview(List<MyEnrollmentRow> Enrollments, List<MyGradeRow> Grades, List<DisciplineAverage> Averages, decimal? OverallAverage);

public sealed record TeacherClassRow(string Name, string CourseName, int Period, int StudentCount);
public sealed record TeacherOverview(List<TeacherClassRow> Classes, int StudentCount, int DisciplineCount, int GradeCount, List<GradeRow> RecentGrades);

// ───────────── Questões (banco do professor e prática do aluno) ─────────────

/// <summary>Questão do banco, já com as alternativas em lista e o índice (0-4) da correta.</summary>
public sealed record QuestionRow(
    Guid Id, Guid DisciplineId, string DisciplineName, string Topic, string Statement,
    List<string> Alternatives, int CorrectIndex, string Explanation, DificuldadeQuestao Difficulty,
    List<SeloDto>? Seals = null, Guid AuthorId = default, int ExamCount = 0, int AppliedExamCount = 0);

/// <summary>NOVO: escolha de um selo no formulário (um item por tipo do catálogo).</summary>
public sealed class SealChoice
{
    public string Code { get; set; } = "";
    public bool Selected { get; set; }
    public int? Year { get; set; }
    public string Detail { get; set; } = "";
    public bool Show { get; set; } = true;
}

/// <summary>NOVO: formulário de edição completa de uma questão (enunciado, alternativas, gabarito, justificativa, assunto, dificuldade, selos).</summary>
public sealed class QuestionEditForm
{
    public Guid DisciplineId { get; set; }
    public string Topic { get; set; } = "";
    public DificuldadeQuestao Difficulty { get; set; } = DificuldadeQuestao.Media;
    public DraftQuestion Content { get; set; } = new();
    public List<SealChoice> Seals { get; set; } = new();
}

/// <summary>Rascunho de questão: vem da IA (ou do formulário manual) e ainda não foi salvo.</summary>
public sealed class DraftQuestion
{
    public string Statement { get; set; } = "";
    public List<string> Alternatives { get; set; } = new() { "", "", "", "", "" };
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = "";
    public bool Selected { get; set; } = true;
}

public sealed record PracticeDiscipline(Guid Id, string Name, string TeacherName, int Total, int Easy, int Medium, int Hard);

/// <summary>Questão da prática: as alternativas já vêm embaralhadas e CorrectIndex vale para a ordem embaralhada.</summary>
public sealed record PracticeQuestion(
    Guid Id, string Topic, string Statement, List<string> Alternatives, int CorrectIndex,
    string Explanation, DificuldadeQuestao Difficulty, List<SeloDto>? Seals = null);

// ───────────── Avaliações (provas e simulados) ─────────────

/// <summary>Dados do cartão de uma avaliação. CanEdit = a pessoa pode alterar (autor, gestor ou administrador).</summary>
public sealed record ExamCard(
    Guid Id, string Code, string Title, TipoAvaliacao Type, Guid DisciplineId, string DisciplineName,
    Guid AuthorId, string AuthorName, DateTime CreatedAt, int QuestionCount, StatusAvaliacao Status,
    string Details, PeriodicidadeAvaliacao Periodicity, int Duration, bool CanEdit);

public sealed record ExamQuestion(int Order, QuestionRow Question);

public sealed record ExamDetail(ExamCard Card, List<ExamQuestion> Questions);

/// <summary>Formulário de criar/editar avaliação.</summary>
public sealed class ExamForm
{
    public string Title { get; set; } = "";
    public TipoAvaliacao Type { get; set; } = TipoAvaliacao.Prova;
    public Guid DisciplineId { get; set; }
    public string Details { get; set; } = "";
    public PeriodicidadeAvaliacao Periodicity { get; set; } = PeriodicidadeAvaliacao.Unica;
    public int Duration { get; set; } = 1;
}

/// <summary>Avaliação aplicada, como o aluno enxerga em "Praticar".</summary>
public sealed record PracticeExam(Guid Id, string Code, string Title, string DisciplineName, string AuthorName, int QuestionCount, TipoAvaliacao Type);

// ───────────── Instituição e unidades (prédios) ─────────────

public sealed record InstitutionInfo(
    Guid Id, string Name, string Cnpj, string Email, string Phone, string Address, bool Active,
    TipoInstituicao Tipo = TipoInstituicao.Faculdade, string? Sigla = null, Guid? MantenedorId = null, string? MantenedorName = null,
    string Primary = "#1F4E79", string Secondary = "#D6A94B", bool HasLogo = false, long LogoVersion = 0);

/// <summary>NOVO: instituição da mesma rede (mesmo mantenedor). Current = a que está em uso agora.</summary>
public sealed record NetworkInstitution(Guid Id, string Name, string? Sigla, TipoInstituicao Tipo, bool Active, bool Current);

public sealed record MantenedorOption(Guid Id, string Name);

/// <summary>Cartão de uma unidade (prédio) com a contagem de pessoas por papel.</summary>
public sealed record UnitCard(Guid Id, string Name, string Slug, string Sigla, TipoUnidade Type, bool Active, int Students, int Professors, int Managers)
{
    public int People => Students + Professors + Managers;
}

// ALTERADO: ganhou as contagens do perfil do curso de Medicina (alunos e professores), que não depende de prédio.
public sealed record InstitutionOverview(InstitutionInfo? Institution, List<UnitCard> Units, int PeopleWithoutUnit, int MedicineStudents = 0, int MedicineProfessors = 0,
    List<NetworkInstitution>? Network = null, List<MantenedorOption>? Mantenedores = null, int UnlinkedPeople = 0);

/// <summary>Pessoa na lista de lotação (nome próprio para não colidir com o PersonRow da tela Equipe). UnitId nulo = sem unidade definida.</summary>
/// <summary>Linked = tem vínculo ativo com a instituição em uso.</summary>
public sealed record UnitPersonRow(Guid UserId, string Name, string Email, UserRole Role, Guid? UnitId, string? UnitName, string? UnitSlug, bool Linked);

public sealed class InstitutionForm
{
    public string Name { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    // NOVO: categoria (Faculdade/Colégio), sigla e mantenedor. Só o Admin altera tipo e mantenedor.
    public TipoInstituicao Tipo { get; set; } = TipoInstituicao.Faculdade;
    public string Sigla { get; set; } = "";
    public Guid? MantenedorId { get; set; }
    public string NewMantenedorName { get; set; } = "";
}

public sealed class UnitForm
{
    public string Name { get; set; } = "";
    public string Sigla { get; set; } = "";
    public TipoUnidade Type { get; set; } = TipoUnidade.Campus;
}

// ───────────── NOVO: simulados e treinos do aluno (separados das notas oficiais) ─────────────

public sealed record PracticeResultRow(
    Guid Id, string Title, string DisciplineName, OrigemPratica Origin, int Total, int Hits, decimal Score, DateTime At, bool Counted);

/// <summary>Resumo dos simulados/treinos do aluno. CountedAverage considera só os marcados para contabilizar.</summary>
public sealed record PracticeSummary(List<PracticeResultRow> Results, int CountedCount, decimal? CountedAverage, decimal CountedSum = 0, int TotalCount = 0);
