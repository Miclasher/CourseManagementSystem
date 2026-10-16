using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Curriculum;
using SharedKernel;

namespace Application.Curriculum.AddModule;

internal sealed class AddModuleCommandHandler(
    ICourseRepository courses,
    ICurriculumRepository curriculum,
    IUserContext userContext)
    : ICommandHandler<AddModuleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddModuleCommand command, CancellationToken cancellationToken)
    {
        Course? course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return Result.Failure<Guid>(CourseErrors.NotFound(command.CourseId));
        }

        if (!await courses.IsInstructorAsync(course.Id, userContext.UserId, cancellationToken))
        {
            return Result.Failure<Guid>(CourseErrors.NotInstructor(course.Id));
        }

        var module = Module.Create(course.Id, command.Title);

        curriculum.AddModule(module);

        return module.Id;
    }
}
