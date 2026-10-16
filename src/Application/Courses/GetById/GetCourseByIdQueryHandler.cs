using System.Data.Common;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Dapper;
using Domain.Courses;
using SharedKernel;

namespace Application.Courses.GetById;

internal sealed class GetCourseByIdQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCourseByIdQuery, CourseResponse>
{
    public async Task<Result<CourseResponse>> Handle(GetCourseByIdQuery query, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        CourseResponse? course = await connection.QuerySingleOrDefaultAsync<CourseResponse>(new CommandDefinition(
            """
            SELECT id, title, slug, description, level, price, rating_avg, rating_count, enrolled_count,
                   category_name, lead_instructor_name, lesson_count, total_minutes
            FROM v_course_catalog
            WHERE id = @CourseId
            """,
            query,
            cancellationToken: cancellationToken));

        if (course is null)
        {
            return Result.Failure<CourseResponse>(CourseErrors.NotFound(query.CourseId));
        }

        IEnumerable<CurriculumRow> rows = await connection.QueryAsync<CurriculumRow>(new CommandDefinition(
            """
            SELECT module_id, module_title, module_position, lesson_id, lesson_title, lesson_position, duration_minutes
            FROM v_course_curriculum
            WHERE course_id = @CourseId
            ORDER BY module_position, lesson_position
            """,
            query,
            cancellationToken: cancellationToken));

        course.Modules = rows
            .GroupBy(row => new { row.ModuleId, row.ModuleTitle, row.ModulePosition })
            .Select(group => new ModuleResponse
            {
                Id = group.Key.ModuleId,
                Title = group.Key.ModuleTitle,
                Position = group.Key.ModulePosition,
                Lessons = group
                    .Where(row => row.LessonId is not null)
                    .Select(row => new LessonResponse
                    {
                        Id = row.LessonId!.Value,
                        Title = row.LessonTitle!,
                        Position = row.LessonPosition!.Value,
                        DurationMinutes = row.DurationMinutes!.Value
                    })
                    .ToList()
            })
            .ToList();

        return course;
    }
}
