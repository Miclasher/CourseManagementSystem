namespace Application.Reports.CourseDashboard;

public interface ICourseDashboardSource
{
    /// <summary>Null when the course does not exist or is soft-deleted.</summary>
    Task<CourseDashboardData?> GetAsync(Guid courseId, CancellationToken cancellationToken);
}
