using SharedKernel;

namespace Domain.Courses;

public static class CourseErrors
{
    public static Error NotFound(Guid courseId) => Error.NotFound(
        "Courses.NotFound",
        $"The course with the Id = '{courseId}' was not found");

    public static Error SlugNotUnique(string slug) => Error.Conflict(
        "Courses.SlugNotUnique",
        $"The slug '{slug}' is already used by another course");

    public static Error NotInstructor(Guid courseId) => Error.Problem(
        "Courses.NotInstructor",
        $"You are not an instructor of the course with the Id = '{courseId}'");

    public static Error AlreadyPublished(Guid courseId) => Error.Problem(
        "Courses.AlreadyPublished",
        $"The course with the Id = '{courseId}' is already published");
}
