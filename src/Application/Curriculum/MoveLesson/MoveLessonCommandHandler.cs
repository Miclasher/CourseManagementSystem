using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Curriculum;
using SharedKernel;

namespace Application.Curriculum.MoveLesson;

internal sealed class MoveLessonCommandHandler(
    ICourseRepository courses,
    ICurriculumRepository curriculum,
    IUserContext userContext)
    : ICommandHandler<MoveLessonCommand>
{
    public async Task<Result> Handle(MoveLessonCommand command, CancellationToken cancellationToken)
    {
        Lesson? lesson = await curriculum.GetLessonByIdAsync(command.LessonId, cancellationToken);

        if (lesson is null)
        {
            return Result.Failure(LessonErrors.NotFound(command.LessonId));
        }

        if (!await courses.IsInstructorAsync(lesson.CourseId, userContext.UserId, cancellationToken))
        {
            return Result.Failure(CourseErrors.NotInstructor(lesson.CourseId));
        }

        curriculum.MoveLesson(lesson, command.Position);

        return Result.Success();
    }
}
