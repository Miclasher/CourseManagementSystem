using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Curriculum;
using SharedKernel;

namespace Application.Curriculum.AddLesson;

internal sealed class AddLessonCommandHandler(
    ICourseRepository courses,
    ICurriculumRepository curriculum,
    IUserContext userContext)
    : ICommandHandler<AddLessonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddLessonCommand command, CancellationToken cancellationToken)
    {
        Module? module = await curriculum.GetModuleByIdAsync(command.ModuleId, cancellationToken);

        if (module is null)
        {
            return Result.Failure<Guid>(ModuleErrors.NotFound(command.ModuleId));
        }

        if (!await courses.IsInstructorAsync(module.CourseId, userContext.UserId, cancellationToken))
        {
            return Result.Failure<Guid>(CourseErrors.NotInstructor(module.CourseId));
        }

        var lesson = Lesson.Create(module.Id, command.Title, command.Content, command.DurationMinutes);

        curriculum.AddLesson(lesson);

        return lesson.Id;
    }
}
