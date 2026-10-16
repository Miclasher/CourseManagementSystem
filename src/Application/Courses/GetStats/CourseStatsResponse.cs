namespace Application.Courses.GetStats;

public sealed class CourseStatsResponse
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Enrolled { get; set; }
    public int Completed { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal RatingAvg { get; set; }
}
