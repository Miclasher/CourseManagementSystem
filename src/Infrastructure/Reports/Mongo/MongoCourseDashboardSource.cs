using Application.Reports.CourseDashboard;
using MongoDB.Driver;

namespace Infrastructure.Reports.Mongo;

internal sealed class MongoCourseDashboardSource(MongoReportsContext context) : ICourseDashboardSource
{
    public async Task<CourseDashboardData?> GetAsync(Guid courseId, CancellationToken cancellationToken) =>
        await context.CourseDashboards
            .Find(d => d.CourseId == courseId)
            .FirstOrDefaultAsync(cancellationToken);
}
