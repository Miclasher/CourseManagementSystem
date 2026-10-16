using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Enrollments;
using SharedKernel;

namespace Application.Enrollments.Enroll;

internal sealed class EnrollCommandHandler(
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IUserContext userContext)
    : ICommandHandler<EnrollCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EnrollCommand command, CancellationToken cancellationToken)
    {
        Course? course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return Result.Failure<Guid>(CourseErrors.NotFound(command.CourseId));
        }

        if (course.Status != CourseStatus.Published)
        {
            return Result.Failure<Guid>(EnrollmentErrors.CourseNotPublished(course.Id));
        }

        Enrollment? existing = await enrollments.GetCurrentAsync(course.Id, userContext.UserId, cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(EnrollmentErrors.AlreadyEnrolled(course.Id));
        }

        if (course.IsFull)
        {
            return Result.Failure<Guid>(EnrollmentErrors.CourseFull(course.Id));
        }

        var enrollment = Enrollment.Create(course.Id, userContext.UserId);

        enrollments.Add(enrollment);

        return enrollment.Id;
    }
}
