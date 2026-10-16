using SharedKernel;

namespace Domain.Courses;

public sealed class Course : Entity
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CourseLevel Level { get; set; }
    public CourseStatus Status { get; set; }
    public decimal Price { get; set; }
    public int? MaxStudents { get; set; }
    public int EnrolledCount { get; set; }
    public DateTime? PublishedAt { get; set; }

    public static Course Create(
        Guid categoryId,
        string title,
        string slug,
        string description,
        CourseLevel level,
        decimal price,
        int? maxStudents)
    {
        var course = new Course
        {
            Id = Guid.CreateVersion7(),
            CategoryId = categoryId,
            Title = title,
            Slug = slug,
            Description = description,
            Level = level,
            Status = CourseStatus.Draft,
            Price = price,
            MaxStudents = maxStudents
        };

        course.Raise(new CourseCreatedDomainEvent(course.Id));

        return course;
    }

    public void Publish()
    {
        Status = CourseStatus.Published;
        PublishedAt ??= DateTime.UtcNow;

        Raise(new CoursePublishedDomainEvent(Id));
    }

    public void MarkDeleted() => Raise(new CourseDeletedDomainEvent(Id));

    public bool IsFull => MaxStudents is not null && EnrolledCount >= MaxStudents;
}
