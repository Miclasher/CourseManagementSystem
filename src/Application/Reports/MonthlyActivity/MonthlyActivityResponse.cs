namespace Application.Reports.MonthlyActivity;

public sealed record MonthlyActivityResponse(int Year, IReadOnlyList<MonthlyActivityRow> Rows);

public sealed record MonthlyActivityRow(
    string Month,
    Guid CategoryId,
    string CategoryName,
    long LessonsCompleted,
    long QuizAttempts,
    long QuizPassed,
    decimal PassRate,
    decimal AvgQuizScore,
    long ActiveStudents);
