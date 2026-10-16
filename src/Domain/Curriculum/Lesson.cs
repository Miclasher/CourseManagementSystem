using SharedKernel;

namespace Domain.Curriculum;

public sealed class Lesson : Entity
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Position { get; set; }

    public static Lesson Create(Guid moduleId, string title, string content, int durationMinutes)
    {
        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            ModuleId = moduleId,
            Title = title,
            Content = content,
            DurationMinutes = durationMinutes
        };

        lesson.Raise(new LessonAddedDomainEvent(lesson.Id, moduleId));

        return lesson;
    }
}
