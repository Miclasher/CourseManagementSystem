using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Assignments.GetPending;

internal sealed class GetPendingSubmissionsQueryHandler(ISqlConnectionFactory connectionFactory, IUserContext userContext)
    : IQueryHandler<GetPendingSubmissionsQuery, List<PendingSubmissionResponse>>
{
    public async Task<Result<List<PendingSubmissionResponse>>> Handle(
        GetPendingSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<PendingSubmissionResponse> submissions = await connection.QueryAsync<PendingSubmissionResponse>(
            new CommandDefinition(
                """
                SELECT submission_id, submitted_at, content, assignment_id, assignment_title,
                       max_score, course_title, student_name
                FROM v_instructor_pending_submissions
                WHERE instructor_id = @UserId
                ORDER BY submitted_at
                """,
                new { userContext.UserId },
                cancellationToken: cancellationToken));

        return submissions.ToList();
    }
}
