namespace Application.Reports.TopCourses;

/// <summary>Raw aggregate for one course over [from, to + 1 day) UTC; same event definition as monthly activity.</summary>
public sealed class TopCourseRaw
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public long LessonsCompleted { get; set; }
    public long QuizAttempts { get; set; }
    public long QuizPassed { get; set; }
    public long ScoreSumHundredths { get; set; }
    public long ActiveStudents { get; set; }
}
