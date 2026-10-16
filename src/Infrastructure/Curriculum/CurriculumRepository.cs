using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Curriculum;

namespace Infrastructure.Curriculum;

internal sealed class CurriculumRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICurriculumRepository
{
    public async Task<Module?> GetModuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Module>(new CommandDefinition(
            "SELECT id, course_id, title, position FROM v_modules WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Lesson>(new CommandDefinition(
            "SELECT id, module_id, course_id, title, content, duration_minutes, position FROM v_active_lessons WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public void AddModule(Module module) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_add_module(@Id::uuid, @CourseId::uuid, @Title::varchar, @actor::uuid)",
                new { module.Id, module.CourseId, module.Title, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            module);

    public void AddLesson(Lesson lesson) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_add_lesson(@Id::uuid, @ModuleId::uuid, @Title::varchar, @Content::text, @DurationMinutes::integer, @actor::uuid)",
                new { lesson.Id, lesson.ModuleId, lesson.Title, lesson.Content, lesson.DurationMinutes, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            lesson);

    public void MoveLesson(Lesson lesson, int newPosition) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_move_lesson(@id::uuid, @newPosition::integer, @actor::uuid)",
                new { id = lesson.Id, newPosition, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            lesson);
}
