using EduAssess.Models;

namespace EduAssess.Web;

public sealed record PersonRow(Guid Id, string Name, string Email, string Phone);
public sealed record StaffRow(Guid Id, string Name, string Email, UserRole Role);
public sealed record InstructorRow(Guid Id, string Name);
public sealed record CourseRow(Guid Id, string Name, string Description, int Duration, int ClassCount);
public sealed record DisciplineRow(Guid Id, string Name, string Description, Guid TeacherId, string TeacherName);
public sealed record ClassRow(Guid Id, string Name, string CourseName, string InstructorName, int Period, int StudentCount);
public sealed record EnrollmentRow(Guid Id, string StudentName, string ClassName, string CourseName, DateTime EnrolledAt, bool Active);
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
