using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using SharedKernel;

namespace Application.Enrollments.GetMine;

internal sealed class GetMyEnrollmentsQueryHandler(ISqlConnectionFactory connectionFactory, IUserContext userContext)
    : IQueryHandler<GetMyEnrollmentsQuery, List<EnrollmentResponse>>
{
    public async Task<Result<List<EnrollmentResponse>>> Handle(
        GetMyEnrollmentsQuery query,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<EnrollmentResponse> enrollments = await connection.QueryAsync<EnrollmentResponse>(
            new CommandDefinition(
                """
                SELECT enrollment_id, course_id, course_title, status, progress_percent,
                       enrolled_at, completed_at, certificate_code
                FROM v_student_enrollments
                WHERE user_id = @UserId
                ORDER BY enrolled_at DESC
                """,
                new { userContext.UserId },
                cancellationToken: cancellationToken));

        return enrollments.ToList();
    }
}
