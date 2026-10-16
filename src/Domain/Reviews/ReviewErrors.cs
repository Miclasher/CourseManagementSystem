using SharedKernel;

namespace Domain.Reviews;

public static class ReviewErrors
{
    public static Error NotEnrolled(Guid courseId) => Error.Problem(
        "Reviews.NotEnrolled",
        $"Only students enrolled in the course with the Id = '{courseId}' can review it");

    public static Error AlreadyReviewed(Guid courseId) => Error.Conflict(
        "Reviews.AlreadyReviewed",
        $"You have already reviewed the course with the Id = '{courseId}'");
}
