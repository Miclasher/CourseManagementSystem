namespace Application.Reports.CourseDashboard;

/// <summary>
/// Raw dashboard of one non-deleted course. This exact shape is also the MongoDB document
/// (collection <c>course_dashboards</c>, <c>_id</c> = CourseId as string), produced by running the PostgreSQL source.
/// </summary>
public sealed class CourseDashboardData
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long PriceCents { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    /// <summary>"First Last" of the lead instructor, null when the course has none.</summary>
    public string? LeadInstructorName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long ActiveEnrollments { get; set; }
    public long CompletedEnrollments { get; set; }
    public long DroppedEnrollments { get; set; }
    /// <summary>Count of non-deleted reviews per rating: [0] = rating 1 ... [4] = rating 5.</summary>
    public List<long> RatingCounts { get; set; } = [];
    /// <summary>All modules with their non-deleted lessons.</summary>
    public List<DashboardModuleData> Modules { get; set; } = [];
    /// <summary>Latest 5 non-deleted reviews by created_at DESC, then review id text ASC.</summary>
    public List<DashboardReviewData> RecentReviews { get; set; } = [];
}

public sealed class DashboardModuleData
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<DashboardLessonData> Lessons { get; set; } = [];
}

public sealed class DashboardLessonData
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class DashboardReviewData
{
    public Guid ReviewId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
