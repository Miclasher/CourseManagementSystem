using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using SharedKernel;

namespace Application.Courses.Publish;

internal sealed class PublishCourseCommandHandler(ICourseRepository courses, IUserContext userContext)
    : ICommandHandler<PublishCourseCommand>
{
    public async Task<Result> Handle(PublishCourseCommand command, CancellationToken cancellationToken)
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

        if (course.Status == CourseStatus.Published)
        {
            return Result.Failure(CourseErrors.AlreadyPublished(course.Id));
        }

        course.Publish();
        courses.Publish(course);

        return Result.Success();
    }
}
