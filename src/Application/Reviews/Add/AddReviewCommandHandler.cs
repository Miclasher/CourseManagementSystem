using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Courses;
using Domain.Enrollments;
using Domain.Reviews;
using SharedKernel;

namespace Application.Reviews.Add;

internal sealed class AddReviewCommandHandler(
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IReviewRepository reviews,
    IUserContext userContext)
    : ICommandHandler<AddReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddReviewCommand command, CancellationToken cancellationToken)
    {
        Course? course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return Result.Failure<Guid>(CourseErrors.NotFound(command.CourseId));
        }

        Enrollment? enrollment = await enrollments.GetCurrentAsync(course.Id, userContext.UserId, cancellationToken);

        if (enrollment is null)
        {
            return Result.Failure<Guid>(ReviewErrors.NotEnrolled(course.Id));
        }

        var review = Review.Create(course.Id, userContext.UserId, command.Rating, command.Comment);

        reviews.Add(review);

        return review.Id;
    }
}
