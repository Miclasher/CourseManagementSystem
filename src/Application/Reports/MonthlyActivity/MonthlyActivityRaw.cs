namespace Application.Reports.MonthlyActivity;

/// <summary>
/// Raw aggregate for one (month, category): events are lesson completions (lesson_progress.completed_at)
/// and quiz attempts (quiz_attempts.attempted_at) of non-deleted courses, bucketed by UTC month.
/// </summary>
public sealed class MonthlyActivityRaw
{
    public int YearMonth { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long LessonsCompleted { get; set; }
    public long QuizAttempts { get; set; }
    public long QuizPassed { get; set; }
    /// <summary>Sum of quiz score_percent * 100.</summary>
    public long ScoreSumHundredths { get; set; }
    /// <summary>Exact number of distinct users with at least one event (of either kind) in the bucket.</summary>
    public long ActiveStudents { get; set; }
}
