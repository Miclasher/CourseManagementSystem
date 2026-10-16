using SharedKernel;

namespace Domain.Assignments;

public sealed class Assignment : Entity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public DateTime? DueAt { get; set; }

    public static Assignment Create(Guid lessonId, string title, string description, int maxScore, DateTime? dueAt)
    {
        var assignment = new Assignment
        {
            Id = Guid.CreateVersion7(),
            LessonId = lessonId,
            Title = title,
            Description = description,
            MaxScore = maxScore,
            DueAt = dueAt
        };

        assignment.Raise(new AssignmentCreatedDomainEvent(assignment.Id));

        return assignment;
    }
}
