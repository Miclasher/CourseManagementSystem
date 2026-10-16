using System.Data.Common;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Courses.GetDeleted;

internal sealed class GetDeletedCoursesQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetDeletedCoursesQuery, List<DeletedCourseResponse>>
{
    public async Task<Result<List<DeletedCourseResponse>>> Handle(
        GetDeletedCoursesQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<DeletedCourseResponse> courses = await connection.QueryAsync<DeletedCourseResponse>(
            new CommandDefinition(
                "SELECT id, title, slug, deleted_at, deleted_by_name FROM v_deleted_courses ORDER BY deleted_at DESC",
                cancellationToken: cancellationToken));

        return courses.ToList();
    }
}
