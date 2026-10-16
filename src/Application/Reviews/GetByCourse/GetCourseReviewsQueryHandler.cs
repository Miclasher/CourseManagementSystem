using System.Data.Common;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Reviews.GetByCourse;

internal sealed class GetCourseReviewsQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCourseReviewsQuery, List<ReviewResponse>>
{
    public async Task<Result<List<ReviewResponse>>> Handle(
        GetCourseReviewsQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<ReviewResponse> reviews = await connection.QueryAsync<ReviewResponse>(new CommandDefinition(
            """
            SELECT id, rating, comment, author_name, created_at
            FROM v_course_reviews
            WHERE course_id = @CourseId
            ORDER BY created_at DESC
            """,
            query,
            cancellationToken: cancellationToken));

        return reviews.ToList();
    }
}
