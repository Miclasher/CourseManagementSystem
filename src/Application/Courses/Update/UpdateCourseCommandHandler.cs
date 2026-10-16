using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using SharedKernel;

namespace Application.Courses.Update;

internal sealed class UpdateCourseCommandHandler(ICourseRepository courses, IUserContext userContext)
    : ICommandHandler<UpdateCourseCommand>
{
    public async Task<Result> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
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

        course.CategoryId = command.CategoryId;
        course.Title = command.Title;
        course.Description = command.Description;
        course.Level = command.Level;
        course.Price = command.Price;
        course.MaxStudents = command.MaxStudents;

        courses.Update(course);

        return Result.Success();
    }
}
