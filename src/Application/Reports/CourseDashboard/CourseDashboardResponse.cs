namespace Application.Reports.CourseDashboard;

public sealed record CourseDashboardResponse(
    Guid CourseId,
    string Title,
    string Slug,
    string Level,
    string Status,
    decimal Price,
    string CategoryName,
    string? LeadInstructorName,
    string? PublishedAt,
    DashboardEnrollments Enrollments,
    DashboardRatings Ratings,
    int TotalLessons,
    int TotalMinutes,
    IReadOnlyList<DashboardModule> Modules,
    IReadOnlyList<DashboardReview> RecentReviews);

public sealed record DashboardEnrollments(long Active, long Completed, long Dropped, long Total, decimal CompletionRate);

public sealed record DashboardRatings(long Count, decimal Average, IReadOnlyList<long> Histogram);

public sealed record DashboardModule(Guid ModuleId, string Title, int Position, int TotalMinutes, IReadOnlyList<DashboardLesson> Lessons);

public sealed record DashboardLesson(Guid LessonId, string Title, int Position, int DurationMinutes);

public sealed record DashboardReview(Guid ReviewId, string AuthorName, int Rating, string? Comment, string CreatedAt);
