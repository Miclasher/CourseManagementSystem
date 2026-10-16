using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Assignments;

namespace Infrastructure.Assignments;

internal sealed class AssignmentRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : IAssignmentRepository
{
    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Assignment>(new CommandDefinition(
            "SELECT id, lesson_id, title, description, max_score, due_at FROM v_assignments WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<Submission?> GetSubmissionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Submission>(new CommandDefinition(
            """
            SELECT id, assignment_id, enrollment_id, content, score, feedback, graded_by, graded_at, submitted_at
            FROM v_submissions WHERE id = @id
            """,
            new { id },
            cancellationToken: cancellationToken));
    }

    public void Add(Assignment assignment) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_create_assignment(@Id::uuid, @LessonId::uuid, @Title::varchar, @Description::text, " +
                "@MaxScore::integer, @DueAt::timestamptz, @actor::uuid)",
                new
                {
                    assignment.Id,
                    assignment.LessonId,
                    assignment.Title,
                    assignment.Description,
                    assignment.MaxScore,
                    assignment.DueAt,
                    actor = userContext.UserId
                },
                transaction,
                cancellationToken: ct)),
            assignment);

    public void Submit(Submission submission) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_submit_assignment(@Id::uuid, @AssignmentId::uuid, @EnrollmentId::uuid, @Content::text, @actor::uuid)",
                new { submission.Id, submission.AssignmentId, submission.EnrollmentId, submission.Content, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            submission);

    public void Grade(Submission submission) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_grade_submission(@Id::uuid, @Score::integer, @Feedback::text, @actor::uuid)",
                new { submission.Id, submission.Score, submission.Feedback, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            submission);
}
