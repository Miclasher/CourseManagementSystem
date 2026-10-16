using SharedKernel;

namespace Domain.Curriculum;

public sealed class Module : Entity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }

    public static Module Create(Guid courseId, string title)
    {
        var module = new Module { Id = Guid.CreateVersion7(), CourseId = courseId, Title = title };

        module.Raise(new ModuleAddedDomainEvent(module.Id, courseId));

        return module;
    }
}
