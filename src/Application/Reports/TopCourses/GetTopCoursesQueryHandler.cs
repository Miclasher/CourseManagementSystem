using System.Globalization;
using Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Application.Reports.TopCourses;

internal sealed class GetTopCoursesQueryHandler(IServiceProvider serviceProvider)
    : IQueryHandler<GetTopCoursesQuery, TopCoursesResponse>
{
    public async Task<Result<TopCoursesResponse>> Handle(GetTopCoursesQuery query, CancellationToken cancellationToken)
    {
        if (query.To < query.From)
        {
            return Result.Failure<TopCoursesResponse>(ReportErrors.InvalidPeriod);
        }

        if (query.Limit is < 1 or > 100)
        {
            return Result.Failure<TopCoursesResponse>(ReportErrors.InvalidLimit);
        }

        ITopCoursesSource? source = serviceProvider.GetKeyedService<ITopCoursesSource>(query.Source);
        if (source is null)
        {
            return Result.Failure<TopCoursesResponse>(ReportErrors.UnsupportedSource);
        }

        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        IReadOnlyList<TopCourseRaw> raw = await source.GetAsync(from, toExclusive, query.Limit, cancellationToken);

        var courses = raw
            .OrderByDescending(r => r.LessonsCompleted)
            .ThenBy(r => ReportMath.Key(r.CourseId), StringComparer.Ordinal)
            .Select((r, index) => new TopCourseRow(
                index + 1,
                r.CourseId,
                r.CourseTitle,
                r.CategoryName,
                r.LessonsCompleted,
                r.QuizAttempts,
                ReportMath.Percent(r.QuizPassed, r.QuizAttempts),
                ReportMath.AverageHundredths(r.ScoreSumHundredths, r.QuizAttempts),
                r.ActiveStudents))
            .ToList();

        return new TopCoursesResponse(
            query.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            query.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            query.Limit,
            courses);
    }
}
