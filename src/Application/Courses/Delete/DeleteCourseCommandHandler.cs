using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using SharedKernel;

namespace Application.Courses.Delete;

internal sealed class DeleteCourseCommandHandler(ICourseRepository courses, IUserContext userContext)
    : ICommandHandler<DeleteCourseCommand>
{
    public async Task<Result> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
    {
        Course? course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return Result.Failure(CourseErrors.NotFound(command.CourseId));
        }

        if (!await courses.IsInstructorAsync(course.Id, userContext.UserId, cancellationToken))
        {
            return Result.Failure(CourseErrors.NotInstructor(course.Id));
        }

        course.MarkDeleted();
        courses.Delete(course);

        return Result.Success();
    }
}
