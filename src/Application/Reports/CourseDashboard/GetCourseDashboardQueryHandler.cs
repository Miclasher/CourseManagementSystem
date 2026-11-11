using Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Application.Reports.CourseDashboard;

internal sealed class GetCourseDashboardQueryHandler(IServiceProvider serviceProvider)
    : IQueryHandler<GetCourseDashboardQuery, CourseDashboardResponse>
{
    private const int RatingLevels = 5;

    public async Task<Result<CourseDashboardResponse>> Handle(GetCourseDashboardQuery query, CancellationToken cancellationToken)
    {
        ICourseDashboardSource? source = serviceProvider.GetKeyedService<ICourseDashboardSource>(query.Source);
        if (source is null)
        {
            return Result.Failure<CourseDashboardResponse>(ReportErrors.UnsupportedSource);
        }

        CourseDashboardData? data = await source.GetAsync(query.CourseId, cancellationToken);
        if (data is null)
        {
            return Result.Failure<CourseDashboardResponse>(ReportErrors.CourseNotFound(query.CourseId));
        }

        return Map(data);
    }

    internal static CourseDashboardResponse Map(CourseDashboardData data)
    {
        long[] histogram = new long[RatingLevels];
        for (int i = 0; i < RatingLevels && i < data.RatingCounts.Count; i++)
        {
            histogram[i] = data.RatingCounts[i];
        }

        long ratingCount = histogram.Sum();
        long ratingSum = histogram.Select((count, index) => count * (index + 1)).Sum();

        var modules = data.Modules
            .OrderBy(m => m.Position)
            .ThenBy(m => ReportMath.Key(m.ModuleId), StringComparer.Ordinal)
            .Select(m =>
            {
                var lessons = m.Lessons
                    .OrderBy(l => l.Position)
                    .ThenBy(l => ReportMath.Key(l.LessonId), StringComparer.Ordinal)
                    .Select(l => new DashboardLesson(l.LessonId, l.Title, l.Position, l.DurationMinutes))
                    .ToList();
                return new DashboardModule(m.ModuleId, m.Title, m.Position, lessons.Sum(l => l.DurationMinutes), lessons);
            })
            .ToList();

        var reviews = data.RecentReviews
            .OrderByDescending(r => ReportMath.Timestamp(r.CreatedAt), StringComparer.Ordinal)
            .ThenBy(r => ReportMath.Key(r.ReviewId), StringComparer.Ordinal)
            .Select(r => new DashboardReview(r.ReviewId, r.AuthorName, r.Rating, r.Comment, ReportMath.Timestamp(r.CreatedAt)))
            .ToList();

        long enrolled = data.ActiveEnrollments + data.CompletedEnrollments;

        return new CourseDashboardResponse(
            data.CourseId,
            data.Title,
            data.Slug,
            data.Level,
            data.Status,
            ReportMath.FromHundredths(data.PriceCents),
            data.CategoryName,
            data.LeadInstructorName,
            ReportMath.Timestamp(data.PublishedAt),
            new DashboardEnrollments(
                data.ActiveEnrollments,
                data.CompletedEnrollments,
                data.DroppedEnrollments,
                enrolled + data.DroppedEnrollments,
                ReportMath.Percent(data.CompletedEnrollments, enrolled)),
            new DashboardRatings(ratingCount, ReportMath.Average(ratingSum, ratingCount), histogram),
            modules.Sum(m => m.Lessons.Count),
            modules.Sum(m => m.TotalMinutes),
            modules,
            reviews);
    }
}
