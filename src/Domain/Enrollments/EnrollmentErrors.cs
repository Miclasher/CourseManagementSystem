using SharedKernel;

namespace Domain.Enrollments;

public static class EnrollmentErrors
{
    public static Error NotFound(Guid enrollmentId) => Error.NotFound(
        "Enrollments.NotFound",
        $"The enrollment with the Id = '{enrollmentId}' was not found");

    public static Error AlreadyEnrolled(Guid courseId) => Error.Conflict(
        "Enrollments.AlreadyEnrolled",
        $"You are already enrolled in the course with the Id = '{courseId}'");

    public static Error CourseFull(Guid courseId) => Error.Conflict(
        "Enrollments.CourseFull",
        $"The course with the Id = '{courseId}' has no free places");

    public static Error CourseNotPublished(Guid courseId) => Error.Problem(
        "Enrollments.CourseNotPublished",
        $"The course with the Id = '{courseId}' is not open for enrollment");

    public static Error NotOwner(Guid enrollmentId) => Error.Problem(
        "Enrollments.NotOwner",
        $"The enrollment with the Id = '{enrollmentId}' belongs to another student");
}
