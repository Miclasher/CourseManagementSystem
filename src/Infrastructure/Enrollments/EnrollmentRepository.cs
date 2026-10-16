using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Enrollments;

namespace Infrastructure.Enrollments;

internal sealed class EnrollmentRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : IEnrollmentRepository
{
    private const string Columns = "id, course_id, user_id, status, enrolled_at, completed_at";

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Enrollment>(new CommandDefinition(
            $"SELECT {Columns} FROM v_enrollments WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<Enrollment?> GetCurrentAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<Enrollment>(new CommandDefinition(
            $"SELECT {Columns} FROM v_enrollments WHERE course_id = @courseId AND user_id = @userId AND status <> 'dropped'",
            new { courseId, userId },
            cancellationToken: cancellationToken));
    }

    public void Add(Enrollment enrollment) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_enroll_student(@Id::uuid, @CourseId::uuid, @UserId::uuid, @actor::uuid)",
                new { enrollment.Id, enrollment.CourseId, enrollment.UserId, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            enrollment);

    public void Drop(Enrollment enrollment) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_drop_enrollment(@id::uuid, @actor::uuid)",
                new { id = enrollment.Id, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            enrollment);

    public void CompleteLesson(Enrollment enrollment, Guid lessonId) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_complete_lesson(@enrollmentId::uuid, @lessonId::uuid, @actor::uuid)",
                new { enrollmentId = enrollment.Id, lessonId, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            enrollment);
}
