namespace Application.Reports.TopCourses;

public interface ITopCoursesSource
{
    /// <summary>
    /// Top <paramref name="limit" /> courses ordered by LessonsCompleted DESC, then course id as canonical
    /// lowercase text ASC (the tie-break must be textual so that every database picks the same set).
    /// Courses with no event in the period are not returned.
    /// </summary>
    Task<IReadOnlyList<TopCourseRaw>> GetAsync(DateTime fromUtc, DateTime toUtcExclusive, int limit, CancellationToken cancellationToken);
}
