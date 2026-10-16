using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Courses;
using Infrastructure.Database;

namespace Infrastructure.Courses;

internal sealed class CourseRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICourseRepository
{
    private const string Columns =
        "id, category_id, title, slug, description, level, status, price, max_students, enrolled_count, published_at";

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Course>(new CommandDefinition(
            $"SELECT {Columns} FROM v_active_courses WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<Course?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Course>(new CommandDefinition(
            $"SELECT {Columns} FROM v_deleted_courses WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> IsInstructorAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM v_course_instructors WHERE course_id = @courseId AND user_id = @userId)",
            new { courseId, userId },
            cancellationToken: cancellationToken));
    }

    public void Add(Course course) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_create_course(@Id::uuid, @CategoryId::uuid, @Title::varchar, @Slug::varchar, @Description::text, " +
                "@Level::varchar, @Price::numeric, @MaxStudents::integer, @actor::uuid)",
                new
                {
                    course.Id,
                    course.CategoryId,
                    course.Title,
                    course.Slug,
                    course.Description,
                    Level = course.Level.ToDbValue(),
                    course.Price,
                    course.MaxStudents,
                    actor = userContext.UserId
                },
                transaction,
                cancellationToken: ct)),
            course);

    public void Update(Course course) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_update_course(@Id::uuid, @CategoryId::uuid, @Title::varchar, @Description::text, " +
                "@Level::varchar, @Price::numeric, @MaxStudents::integer, @actor::uuid)",
                new
                {
                    course.Id,
                    course.CategoryId,
                    course.Title,
                    course.Description,
                    Level = course.Level.ToDbValue(),
                    course.Price,
                    course.MaxStudents,
                    actor = userContext.UserId
                },
                transaction,
                cancellationToken: ct)),
            course);

    public void Publish(Course course) => Call("CALL sp_publish_course(@id::uuid, @actor::uuid)", course);

    public void Delete(Course course) => Call("CALL sp_soft_delete_course(@id::uuid, @actor::uuid)", course);

    public void Restore(Course course) => Call("CALL sp_restore_course(@id::uuid, @actor::uuid)", course);

    private void Call(string sql, Course course) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { id = course.Id, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            course);
}
