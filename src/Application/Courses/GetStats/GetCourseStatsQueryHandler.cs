using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Courses.GetStats;

internal sealed class GetCourseStatsQueryHandler(ISqlConnectionFactory connectionFactory, IUserContext userContext)
    : IQueryHandler<GetCourseStatsQuery, List<CourseStatsResponse>>
{
    public async Task<Result<List<CourseStatsResponse>>> Handle(
        GetCourseStatsQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<CourseStatsResponse> stats = await connection.QueryAsync<CourseStatsResponse>(new CommandDefinition(
            """
            SELECT s.course_id, s.title, s.enrolled, s.completed, s.completion_rate, s.rating_avg
            FROM v_course_stats s
            JOIN v_course_instructors ci ON ci.course_id = s.course_id
            WHERE ci.user_id = @UserId
            ORDER BY s.title
            """,
            new { userContext.UserId },
            cancellationToken: cancellationToken));

        return stats.ToList();
    }
}
