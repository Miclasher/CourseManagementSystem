using SharedKernel;

namespace Domain.Curriculum;

public static class LessonErrors
{
    public static Error NotFound(Guid lessonId) => Error.NotFound(
        "Lessons.NotFound",
        $"The lesson with the Id = '{lessonId}' was not found");
}
